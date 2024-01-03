// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bicep.Core.Analyzers.Linter.Rules;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.Navigation;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.TypeSystem.Types;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Utils;
using Bicep.Core.Utils;
using Bicep.Core.Workspaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bicep.Core.UnitTests.Diagnostics.LinterRuleTests
{
    [TestClass]
    public class ExplicitValuesForLocationParamsRuleTests : LinterRuleTestsBase
    {
        [TestMethod]
        public void If_ModuleHas_NoLocationParam_ShouldPass()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param noLocationParameter string = 'hello'
                    output o string = noLocationParameter
                   ")
            );
            result.Should().NotHaveAnyDiagnostics();
        }

        [TestMethod]
        public void If_ModuleHas_LocationParam_WithoutDefault_AndValuePassedIn_ShouldPass()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        location: location
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param location string
                    output o string = location
                   ")
            );
            result.Should().NotHaveAnyDiagnostics();
        }

        [TestMethod]
        public void If_ModuleHas_LocationParam_WithoutDefault_AndValueNotPassedIn_ShouldHaveCompilerError_AndNoLinterError()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m2 'module1.bicep' = {
                      name: 'm1'
                      params: {
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param location string
                    output o string = location
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                ("BCP035", DiagnosticLevel.Error, "The specified \"object\" declaration is missing the following required properties: \"location\"."),
            });
        }

        [TestMethod]
        public void If_ModuleHas_LocationParam_WithRGLocationDefault_AndValuePassedIn_ShouldPass()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        p1: location
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param p1 string = resourceGroup().location
                    output o string = p1
                   ")
            );
            result.Diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public void If_ModuleHas_LocationParam_WithDeploymentLocDefault_AndValuePassedIn_ShouldPass()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    targetScope = 'subscription'

                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        p1: location
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    targetScope = 'subscription'
                    param p1 string = deployment().location
                    output o string = p1
                   ")
            );
            result.Diagnostics.Should().BeEmpty();
        }

        [TestMethod]
        public void If_ModuleHas_LocationParam_WithRGLocationDefault_AndValueNotPassedIn_ShouldFail()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m3 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        // FAILURE: p1 not passed in
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param p1 string = resourceGroup().location
                    output o string = p1
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p1' of module 'm3' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
            });
        }

        [TestMethod]
        public void MultipleInstances_OfSameModule()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        // FAILURE: p1 not passed in
                        // FAILURE: p2 not passed in
                      }
                    }

                    module m2 'module1.bicep' = {
                      name: 'm2'
                      params: {
                        // FAILURE: p1 not passed in
                        // FAILURE: p2 not passed in
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param p1 string = resourceGroup().location
                    param p2 string = resourceGroup().location
                    output o string = p1
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p1' of module 'm1' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p2' of module 'm1' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p1' of module 'm2' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p2' of module 'm2' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
            });
        }

        [TestMethod]
        public void If_ModuleHas_LocationParams_WithRGLocationDefault_AndValuesNotPassedIn_ShouldFail()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m3 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        // FAILURE: p1 and p2 not passed in
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param p1 string = resourceGroup().location
                    param p2 string = deployment().location
                    output o string = '${p1}${p2}'
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
              ("BCP104", DiagnosticLevel.Error, "The referenced module has errors."),
              (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p1' of module 'm3' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
              (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p2' of module 'm3' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),});
        }

        [TestMethod]
        public void If_ModuleHas_LocationParams_UsedInResourceLocation_WithDefaultValues_AndValuesNotPassedIn_ShouldFail()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m 'module1.bicep' = {
                      name: 'name'
                      params: {
                        // FAILURE: p1, p2, p3 and p4 not passed in
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param p1 string = resourceGroup().location // references resourceGroup().location
                    param p2 string = concat(az.resourceGroup().location) // references resourceGroup().location
                    param p3 string = 'anyvalue' // used in a resource's location property
                    param p4 string = true ? 'anyvalue' : 'anyothervalue' // used in a resource's location property
                    param pNotUsedInResLocation string = 'anyvalue'
                    param pWithNoDefault string
                    param pWithoutResourceGroupLocationDefault string = resourceGroup().id

                    resource appInsightsComponents 'Microsoft.Insights/components@2020-02-02-preview' = {
                        name: 'name'
                        location: '${p1}${p2}${p3}${pWithNoDefault}'
                        kind: 'web'
                        properties: {
                        Application_Type: 'web'
                        }
                    }

                    resource appInsightsComponents2 'Microsoft.Insights/components@2020-02-02-preview' = {
                        name: 'name2'
                        location: p4
                        kind: 'web'
                        properties: {
                        Application_Type: 'web'
                        }
                    }

                    output s string = '${p1}${p2}${p3}${p4}${pWithNoDefault}${pNotUsedInResLocation}${pWithoutResourceGroupLocationDefault}'
                  ")
            );

            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
              ("BCP035", DiagnosticLevel.Error, "The specified \"object\" declaration is missing the following required properties: \"pWithNoDefault\"."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p1' of module 'm' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p2' of module 'm' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p3' of module 'm' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'p4' of module 'm' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
             });
        }

        [TestMethod]
        public void If_ModuleHas_LocationParam_WithDeploymentLocDefault_AndValueNotPassedIn_CaseInsensitive_ShouldFail()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    targetScope = 'subscription'

                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    targetScope = 'subscription'
                    param myParam string = deployment().location
                    output o string = myParam
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'myParam' of module 'm1' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),

            });
        }

        [TestMethod]
        public void If_Module_HasErrors_LocationParam_WithDefault_AndValuePassedIn_CaseInsensitive_ShouldPass()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                        LOCATION: location
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    param LOCATION string
                    output o string = whoops // error
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                ("BCP104", DiagnosticLevel.Error, "The referenced module has errors.")
            });
        }

        [TestMethod]
        public void ForLoop3_Module()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    module m2 'module1.bicep' = [for i in range(0, 10): {
                        name: 'name${i}'
                    }]"),
                ("module1.bicep", @"
                    param location string = resourceGroup().location
                    output o string = location
                   ")
            );

            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning,  "Parameter 'location' of module 'm2' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
            });
        }

        [TestMethod]
        public void Conditional1_Module()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    param deploy bool
                    module m3 'module1.bicep' = [for i in range(0, 10): if (deploy) {
                      name: 'name${i}'
                    }]
                "),
                ("module1.bicep", @"
                    param location string = resourceGroup().location
                    output o string = location
                   ")
            );

            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'location' of module 'm3' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),
            });
        }

        [TestMethod]
        public void CantBeFooledByStrings()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    module m3 'module1.bicep' = {
                      name: 'name'
                    }
                "),
                ("module1.bicep", @"
                    param location string = 'resourceGroup().location' // *not* a location-related param
                    output o string = location
                   ")
            );

            result.Diagnostics.Should().NotHaveAnyDiagnostics();
        }

        [TestMethod]
        public void asdfg()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    module m1a 'module1.bicep' = {
                      name: 'name1a'
                    }
                    module m2 'abc/../module2.bicep' = {
                      name: 'name2'
                    }
                    module m1b 'abc/../module1.bicep' = {
                      name: 'name1b'
                    }
                "),
                ("module1.bicep", @"
                    param location string = 'resourceGroup().location' // *not* a location-related param
                    output o string = location
                    module m2 'abc/../module2.bicep' = {
                      name: 'name2'
                    }
                   "),
                ("module2.bicep", @"
                    param location string = 'resourceGroup().location' // *not* a location-related param
                    output o string = location
                   ")
            );

            foreach (var sourceAndDictPair in result.Compilation.SourceFileGrouping.FileUriResultByArtifactReference)
            {
                ISourceFile referencingFile = sourceAndDictPair.Key;
                IDictionary<IArtifactReferenceSyntax, Result<Uri, UriResolutionError>> referenceSyntaxeToUri = sourceAndDictPair.Value;

                foreach (var syntaxAndUriPair in referenceSyntaxeToUri)
                {
                    IArtifactReferenceSyntax syntax = syntaxAndUriPair.Key;
                    Result<Uri, UriResolutionError> uriResult = syntaxAndUriPair.Value;
                    if (syntax.Path is { } && uriResult.IsSuccess(out var uri))
                    {
                        Trace.WriteLine($"{referencingFile.FileUri}: {syntax.Path.ToText()} -> {uri}");
                    }
                }
                // Key - syntax
                // Value - Uri result
                //var referenceSpans = referenceSyntaxes.Keys.Select(x => x.TryGetPath()).WhereNotNull().Select(x => x.Span);
                //var a = referenceSpans;
            }

            //    KeyValuePair<ISourceFile, ImmutableDictionary<IArtifactReferenceSyntax, Utils.Result<System.Uri, UriResolutionError>>>
            //}
            //var moduleSymbols = result.Compilation.GetEntrypointSemanticModel().Root.Declarations.OfType<ModuleSymbol>();
            //foreach (var moduleSymbol in moduleSymbols)
            //{
            //    if (moduleSymbol.DeclaringSyntax is ModuleDeclarationSyntax syntax)
            //    {
            //        var span = syntax.Path.Span;
            //        Trace.WriteLine(span);

            //        //if (moduleSymbol.Type is ModuleType type)
            //        //{
            //        //    var body = type.Body;
            //        //    var b = body;
            //        //    body = b;
            //        //}

            //        //if (moduleSymbol.TryGetModuleType() is ModuleType moduleType)
            //        //{
            //        //    var a = moduleType;
            //        //    var b = a;
            //        //    a = b;
            //        //}

            //        var aa = result.Compilation.SourceFileGrouping.TryGetSourceFile(syntax);
            //        var bb = aa;
            //        aa = bb;

            //        var aaa = result.Compilation.SourceFileGrouping.FileUriResultByArtifactReference.First().Value.First().Key;
            //        var bbb = result.Compilation.SourceFileGrouping.FileUriResultByArtifactReference.First().Value.Skip(1).First().Key;
            //        Trace.WriteLine(aaa.Path?.Span);
            //        Trace.WriteLine(bbb.Path?.Span);
            //    }
            //}

            //var model = result.Compilation.GetEntrypointSemanticModel();
            //var model2 = model;
            //model = model2;
        }

        [TestMethod]
        public void Asdfg2()
        {
            var result = CompilationHelper.Compile(
                ("main.bicep", @"
                    targetScope = 'subscription'

                    param location string

                    module m1 'module1.bicep' = {
                      name: 'm1'
                      params: {
                      }
                    }

                    output o string = location
                    "),
                ("module1.bicep", @"
                    targetScope = 'subscription'
                    param myParam string = deployment().location
                    output o string = myParam
                   ")
            );
            result.Diagnostics.Should().HaveDiagnostics(new[]
            {
                (ExplicitValuesForLocationParamsRule.Code, DiagnosticLevel.Warning, "Parameter 'myParam' of module 'm1' isn't assigned an explicit value, and its default value may not give the intended behavior for a location-related parameter. You should assign an explicit value to the parameter."),

            });
        }
    }
}
