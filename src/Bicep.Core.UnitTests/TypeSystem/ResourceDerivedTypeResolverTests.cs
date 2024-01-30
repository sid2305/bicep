// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Immutable;
using Bicep.Core.Diagnostics;
using Bicep.Core.Extensions;
using Bicep.Core.Features;
using Bicep.Core.Resources;
using Bicep.Core.Semantics;
using Bicep.Core.Semantics.Namespaces;
using Bicep.Core.TypeSystem;
using Bicep.Core.TypeSystem.Providers;
using Bicep.Core.TypeSystem.Types;
using Bicep.Core.UnitTests.Assertions;
using Bicep.Core.UnitTests.Mock;
using Bicep.Core.Workspaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Bicep.Core.UnitTests.TypeSystem;

[TestClass]
public class ResourceDerivedTypeResolverTests
{
    [DataTestMethod]
    [DynamicData(nameof(GetTypesNotInNeedOfBinding), DynamicDataSourceType.Method)]
    public void Returns_input_if_no_unbound_types_are_enclosed(TypeSymbol type)
    {
        ResourceDerivedTypeResolver sut = new(StrictMock.Of<IBinder>().Object);
        sut.ResolveResourceDerivedTypes(type).Should().BeSameAs(type);
    }

    private static IEnumerable<object[]> GetTypesNotInNeedOfBinding()
    {
        static object[] Row(TypeSymbol type) => new object[] { type };

        yield return Row(LanguageConstants.Any);
        yield return Row(LanguageConstants.Null);
        yield return Row(LanguageConstants.Bool);
        yield return Row(LanguageConstants.String);
        yield return Row(LanguageConstants.SecureString);
        yield return Row(LanguageConstants.Int);
        yield return Row(LanguageConstants.Array);
        yield return Row(LanguageConstants.Object);
        yield return Row(LanguageConstants.SecureObject);
        yield return Row(LanguageConstants.SecureString);

        // recursive object
        ObjectType? myObject = null;
        myObject = new("recursive",
            TypeSymbolValidationFlags.Default,
            new TypeProperty("property", new DeferredTypeReference(() => myObject!)).AsEnumerable(),
            null);
        yield return Row(myObject);
    }

    [TestMethod]
    public void Hydrates_array_item_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = TypeFactory.CreateArrayType(new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any));
        sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<TypedArrayType>()
            .Subject.Item.Should().BeSameAs(hydrated);
    }

    [TestMethod]
    public void Hydrates_tuple_item_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = new TupleType(ImmutableArray.Create<ITypeReference>(new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any)),
            TypeSymbolValidationFlags.Default);
        sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<TupleType>()
            .Subject.Items.First().Should().BeSameAs(hydrated);
    }

    [TestMethod]
    public void Hydrates_discriminated_object_variant_types()
    {
        var hydrated = new ObjectType("foo",
            TypeSymbolValidationFlags.Default,
            new TypeProperty("property", TypeFactory.CreateStringLiteralType("foo")).AsEnumerable(),
            null);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = new DiscriminatedObjectType("discriminatedObject",
            TypeSymbolValidationFlags.Default,
            "property",
            new ITypeReference[]
            {
                new UnresolvedResourceDerivedPartialObjectType(unhydratedTypeRef, ImmutableArray<string>.Empty, "property", "foo"),
                new ObjectType("bar",
                    TypeSymbolValidationFlags.Default,
                    new TypeProperty("property", TypeFactory.CreateStringLiteralType("bar")).AsEnumerable(),
                    null)
            });

        sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<DiscriminatedObjectType>()
            .Subject.UnionMembersByKey["'foo'"].Should().BeSameAs(hydrated);
    }

    [TestMethod]
    public void Hydrates_object_property_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = new ObjectType("object",
            TypeSymbolValidationFlags.Default,
            new TypeProperty("property", new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any)).AsEnumerable(),
            null);

        sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<ObjectType>()
            .Subject.Properties["property"].TypeReference.Type.Should().BeSameAs(hydrated);
    }

    [TestMethod]
    public void Hydrates_object_additinalProperties_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = new ObjectType("object",
            TypeSymbolValidationFlags.Default,
            ImmutableArray<TypeProperty>.Empty,
            new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any));

        var hydratedContainer = sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<ObjectType>().Subject;
        hydratedContainer.AdditionalPropertiesType.Should().NotBeNull();
        hydratedContainer.AdditionalPropertiesType!.Type.Should().BeSameAs(hydrated);
    }

    [TestMethod]
    public void Hydrates_union_member_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = TypeHelper.CreateTypeUnion(LanguageConstants.String,
            new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any));

        sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<UnionType>()
            .Subject.Members.Should().Contain(hydrated);
    }

    [TestMethod]
    public void Hydrates_wrapped_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = new TypeType(new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any));

        sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<TypeType>()
            .Subject.Unwrapped.Should().BeSameAs(hydrated);
    }

    [TestMethod]
    public void Hydrates_lambda_types()
    {
        var hydrated = TypeFactory.CreateBooleanLiteralType(false);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        var containsUnresolved = new LambdaType(
            ImmutableArray.Create<ITypeReference>(
                TypeFactory.CreateArrayType(new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any)),
                new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any)),
            new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, LanguageConstants.Any));

        var bound = sut.ResolveResourceDerivedTypes(containsUnresolved).Should().BeOfType<LambdaType>().Subject;
        bound.ArgumentTypes.Should().SatisfyRespectively(
            arg => arg.Type.Should().BeOfType<TypedArrayType>().Subject.Item.Type.Should().BeSameAs(hydrated),
            arg => arg.Type.Should().BeSameAs(hydrated));
        bound.ReturnType.Should().BeSameAs(hydrated);
    }

    private static (ResourceDerivedTypeResolver sut, ResourceTypeReference unhydratedTypeRef) SetupResolver(TypeSymbol hydratedType)
    {
        var unhydratedTypeRef = new ResourceTypeReference("type", "version");
        var resourceTypeProviderMock = StrictMock.Of<IResourceTypeProvider>();
        var stubbedNamespaceType = new NamespaceType(AzNamespaceType.BuiltInName,
            AzNamespaceType.Settings,
            ImmutableArray<TypeProperty>.Empty,
            ImmutableArray<FunctionOverload>.Empty,
            ImmutableArray<BannedFunction>.Empty,
            ImmutableArray<Decorator>.Empty,
            resourceTypeProviderMock.Object);
        resourceTypeProviderMock.Setup(x => x.TryGetDefinedType(stubbedNamespaceType, unhydratedTypeRef, ResourceTypeGenerationFlags.None))
            .Returns(new ResourceType(stubbedNamespaceType,
                unhydratedTypeRef,
                ResourceScope.None,
                ResourceScope.None,
                ResourceFlags.None,
                hydratedType,
                ImmutableHashSet<string>.Empty));

        var namespaceProviderMock = StrictMock.Of<INamespaceProvider>();
        namespaceProviderMock.Setup(x => x.TryGetNamespace(It.Is<ResourceTypesProviderDescriptor>(x => x.NamespaceIdentifier == AzNamespaceType.BuiltInName),
            It.IsAny<ResourceScope>(),
            It.IsAny<IFeatureProvider>(),
            It.IsAny<BicepSourceFileKind>()))
            .Returns(new ResultWithDiagnostic<NamespaceType>(stubbedNamespaceType));
        namespaceProviderMock.Setup(x => x.TryGetNamespace(It.Is<ResourceTypesProviderDescriptor>(x => x.NamespaceIdentifier != AzNamespaceType.BuiltInName),
            It.IsAny<ResourceScope>(),
            It.IsAny<IFeatureProvider>(),
            It.IsAny<BicepSourceFileKind>()))
            .Returns(new ResultWithDiagnostic<NamespaceType>(x => x.UnrecognizedProvider(unhydratedTypeRef.Name)));

        var scopeMock = StrictMock.Of<ILanguageScope>();
        scopeMock.Setup(x => x.GetDeclarationsByName(It.IsAny<string>()))
            .Returns(Enumerable.Empty<DeclaredSymbol>());
        scopeMock.Setup(x => x.Declarations)
            .Returns(Enumerable.Empty<DeclaredSymbol>());

        var resolver = NamespaceResolver.Create(BicepTestConstants.Features,
            namespaceProviderMock.Object,
            SourceFileFactory.CreateBicepFile(new("file:///path/to/main.bicep"), string.Empty),
            ResourceScope.None,
            scopeMock.Object);

        var binderMock = StrictMock.Of<IBinder>();
        binderMock.Setup(x => x.NamespaceResolver).Returns(resolver);

        return (new(binderMock.Object), unhydratedTypeRef);
    }

    [TestMethod]
    public void Emits_diagnostic_and_uses_fallback_type_when_resource_not_found()
    {
        var unhydratedTypeRef = new ResourceTypeReference("type", "version");
        var resourceTypeProviderMock = StrictMock.Of<IResourceTypeProvider>();
        var stubbedNamespaceType = new NamespaceType(AzNamespaceType.BuiltInName,
            AzNamespaceType.Settings,
            ImmutableArray<TypeProperty>.Empty,
            ImmutableArray<FunctionOverload>.Empty,
            ImmutableArray<BannedFunction>.Empty,
            ImmutableArray<Decorator>.Empty,
            resourceTypeProviderMock.Object);
        resourceTypeProviderMock.Setup(x => x.TryGetDefinedType(stubbedNamespaceType, unhydratedTypeRef, ResourceTypeGenerationFlags.None))
            .Returns((ResourceType?)null);
        resourceTypeProviderMock.Setup(x => x.TryGenerateFallbackType(stubbedNamespaceType, unhydratedTypeRef, ResourceTypeGenerationFlags.None))
            .Returns((ResourceType?)null);

        var namespaceProviderMock = StrictMock.Of<INamespaceProvider>();
        namespaceProviderMock.Setup(x => x.TryGetNamespace(It.IsAny<ResourceTypesProviderDescriptor>(),
            It.IsAny<ResourceScope>(),
            It.IsAny<IFeatureProvider>(),
            It.IsAny<BicepSourceFileKind>()))
            .Returns(new ResultWithDiagnostic<NamespaceType>(stubbedNamespaceType));
        namespaceProviderMock.Setup(x => x.TryGetNamespace(It.Is<ResourceTypesProviderDescriptor>(x => x.NamespaceIdentifier != AzNamespaceType.BuiltInName),
            It.IsAny<ResourceScope>(),
            It.IsAny<IFeatureProvider>(),
            It.IsAny<BicepSourceFileKind>()))
            .Returns(new ResultWithDiagnostic<NamespaceType>(x => x.UnrecognizedProvider(unhydratedTypeRef.Name)));

        var scopeMock = StrictMock.Of<ILanguageScope>();
        scopeMock.Setup(x => x.GetDeclarationsByName(It.IsAny<string>()))
            .Returns(Enumerable.Empty<DeclaredSymbol>());
        scopeMock.Setup(x => x.Declarations)
            .Returns(Enumerable.Empty<DeclaredSymbol>());

        var resolver = NamespaceResolver.Create(BicepTestConstants.Features,
            namespaceProviderMock.Object,
            SourceFileFactory.CreateBicepFile(new("file:///path/to/main.bicep"), string.Empty),
            ResourceScope.None,
            scopeMock.Object);

        var binderMock = StrictMock.Of<IBinder>();
        binderMock.Setup(x => x.NamespaceResolver).Returns(resolver);

        ResourceDerivedTypeResolver sut = new(binderMock.Object);
        var fallbackType = LanguageConstants.SecureString;

        sut.ResolveResourceDerivedTypes(new UnresolvedResourceDerivedType(unhydratedTypeRef, ImmutableArray<string>.Empty, fallbackType)).Should().BeSameAs(fallbackType);

        resourceTypeProviderMock.Verify(x => x.TryGetDefinedType(stubbedNamespaceType, unhydratedTypeRef, ResourceTypeGenerationFlags.None), Times.Once());
        resourceTypeProviderMock.Verify(x => x.TryGenerateFallbackType(stubbedNamespaceType, unhydratedTypeRef, ResourceTypeGenerationFlags.None), Times.Once());
    }

    [TestMethod]
    public void Supports_pointers_to_partial_resource_body_types()
    {
        var targetType = TypeFactory.CreateBooleanLiteralType(false);
        var hydrated = new ObjectType("object",
            TypeSymbolValidationFlags.Default,
            ImmutableArray.Create(new TypeProperty("property",
                new TupleType(
                    ImmutableArray.Create<ITypeReference>(new ObjectType("dictionary",
                        TypeSymbolValidationFlags.Default,
                        ImmutableArray<TypeProperty>.Empty,
                        TypeFactory.CreateArrayType(targetType))),
                    TypeSymbolValidationFlags.Default))),
            null);
        var (sut, unhydratedTypeRef) = SetupResolver(hydrated);

        UnresolvedResourceDerivedType unresolved = new(unhydratedTypeRef,
            ImmutableArray.Create("properties", "property", "prefixItems", "0", "additionalProperties", "items"),
            LanguageConstants.Any);

        var bound = sut.ResolveResourceDerivedTypes(unresolved);
        bound.Should().BeSameAs(targetType);
    }
}
