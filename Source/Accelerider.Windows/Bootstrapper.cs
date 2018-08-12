﻿using System;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.Net;
using Accelerider.Windows.Constants;
using Accelerider.Windows.Infrastructure;
using Accelerider.Windows.Infrastructure.Interfaces;
using Accelerider.Windows.Models;
using Accelerider.Windows.Views.Authentication;
using Autofac;
using Prism.Autofac;
using Prism.Mvvm;
using Prism.Logging;
using Prism.Modularity;
using Refit;

namespace Accelerider.Windows
{
    public class Bootstrapper : AutofacBootstrapper
    {
        #region Overridered methods
        protected override ILoggerFacade CreateLogger() => new Logger();

        protected override IModuleCatalog CreateModuleCatalog()
        {
            return new DirectoryModuleCatalog { ModulePath = @".\Modules" };
        }

        protected override void ConfigureContainerBuilder(ContainerBuilder builder)
        {
            base.ConfigureContainerBuilder(builder);
            builder.RegisterInstance(new SnackbarMessageQueue(TimeSpan.FromSeconds(2))).As<ISnackbarMessageQueue>();
            builder.RegisterInstance(new ConfigureFile().Load()).As<IConfigureFile>();
            builder.RegisterInstance(RestService.For<INonAuthenticationApi>(Hyperlinks.ApiBaseAddress)).As<INonAuthenticationApi>();
        }

        protected override void ConfigureViewModelLocator() => ViewModelLocationProvider.SetDefaultViewModelFactory(ResolveViewModel);

        protected override DependencyObject CreateShell() => new AuthenticationWindow();

        protected override void InitializeShell()
        {
            ServicePointManager.DefaultConnectionLimit = int.MaxValue;
            ApiExceptionResolverExtension.SetUnityContainer(Container);
            ConfigureApplicationEventHandlers();
            ShellSwitcher.Show((Window)Shell);
        }
        #endregion

        #region Private methods
        private void ConfigureApplicationEventHandlers()
        {
            var resolver = Container.Resolve<ExceptionResolver>();
            AppDomain.CurrentDomain.UnhandledException += resolver.UnhandledExceptionHandler;
            Application.Current.DispatcherUnhandledException += resolver.DispatcherUnhandledExceptionHandler;

            Application.Current.Exit += OnExit;
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            //Container.Resolve<IAcceleriderUser>().OnExit();
            (Logger as IDisposable)?.Dispose();
        }

        private object ResolveViewModel(object view, Type viewModelType)
        {
            var viewModel = Container.Resolve(viewModelType);
            if (view is FrameworkElement frameworkElement &&
                viewModel is ViewModelBase viewModelBase)
            {
                frameworkElement.Loaded += (sender, e) => viewModelBase.OnLoaded(sender);
                frameworkElement.Unloaded += (sender, e) => viewModelBase.OnUnloaded(sender);
            }
            return viewModel;
        }
        #endregion
    }
}
