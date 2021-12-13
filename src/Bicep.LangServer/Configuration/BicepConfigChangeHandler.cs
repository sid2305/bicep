// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Bicep.Core.Analyzers.Linter;
using Bicep.Core.Configuration;
using Bicep.Core.Workspaces;
using Bicep.LanguageServer.CompilationManager;
using Bicep.LanguageServer.Telemetry;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Bicep.LanguageServer.Configuration
{
    public class BicepConfigChangeHandler : IBicepConfigChangeHandler
    {
        private readonly IWorkspace workspace;
        private readonly ICompilationManager compilationManager;
        private readonly IConfigurationManager configurationManager;
        private readonly ILinterRulesProvider linterRulesProvider;
        private readonly ITelemetryProvider telemetryProvider;

        private readonly ConcurrentDictionary<DocumentUri, RootConfiguration> activeBicepConfigCache = new ConcurrentDictionary<DocumentUri, RootConfiguration>();

        public BicepConfigChangeHandler(IConfigurationManager configurationManager,
                                        ICompilationManager compilationManager,
                                        IWorkspace workspace,
                                        ITelemetryProvider telemetryProvider,
                                        ILinterRulesProvider linterRulesProvider)
        {
            this.configurationManager = configurationManager;
            this.compilationManager = compilationManager;
            this.workspace = workspace;
            this.linterRulesProvider = linterRulesProvider;
            this.telemetryProvider = telemetryProvider;
        }

        public void HandleBicepConfigOpenEvent(DocumentUri documentUri)
        {
            HandleBicepConfigOpenOrChangeEvent(documentUri);
        }

        public void HandleBicepConfigChangeEvent(DocumentUri documentUri)
        {
            HandleBicepConfigOpenOrChangeEvent(documentUri);
        }

        private void HandleBicepConfigOpenOrChangeEvent(DocumentUri documentUri)
        {
            if (ConfigurationHelper.TryGetConfiguration(configurationManager, documentUri, out RootConfiguration? configuration))
            {
                activeBicepConfigCache.AddOrUpdate(documentUri, (documentUri) => configuration, (documentUri, prevConfiguration) => configuration);
            }
        }

        public void HandleBicepConfigSaveEvent(DocumentUri documentUri)
        {
            foreach (Uri sourceFileUri in workspace.GetActiveSourceFilesByUri().Keys)
            {
                compilationManager.RefreshCompilation(DocumentUri.From(sourceFileUri), reloadBicepConfig: true);
            }

            if (activeBicepConfigCache.TryGetValue(documentUri, out RootConfiguration? prevBicepConfiguration) &&
                prevBicepConfiguration != null &&
                ConfigurationHelper.TryGetConfiguration(configurationManager, documentUri, out RootConfiguration? curConfiguration))
            {
                TelemetryHelper.SendTelemetryOnBicepConfigChange(prevBicepConfiguration, curConfiguration, linterRulesProvider, telemetryProvider);
                activeBicepConfigCache.AddOrUpdate(documentUri, (documentUri) => curConfiguration, (documentUri, prevConfiguration) => curConfiguration);
            }
        }

        public void HandleBicepConfigCloseEvent(DocumentUri documentUri)
        {
            activeBicepConfigCache.TryRemove(documentUri, out _);
        }
    }
}
