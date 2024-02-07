// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Bicep.Core.TypeSystem;

namespace Bicep.Core.Semantics
{
    public class DecoratorBuilder(string name)
    {
        private readonly FunctionOverloadBuilder functionOverloadBuilder = new FunctionOverloadBuilder(name);

        private TypeSymbol attachableType = LanguageConstants.Any;

        private DecoratorValidator? validator;

        private DecoratorEvaluator? evaluator;

        public DecoratorBuilder WithDescription(string description)
        {
            this.functionOverloadBuilder.WithDescription(description);

            return this;
        }

        public DecoratorBuilder WithRequiredParameter(string name, TypeSymbol type, string description)
        {
            this.functionOverloadBuilder.WithRequiredParameter(name, type, description);

            return this;
        }

        public DecoratorBuilder WithOptionalParameter(string name, TypeSymbol type, string description)
        {
            this.functionOverloadBuilder.WithOptionalParameter(name, type, description);

            return this;
        }

        public DecoratorBuilder WithVariableParameter(string namePrefix, TypeSymbol type, int minimumCount, string description)
        {
            this.functionOverloadBuilder.WithVariableParameter(namePrefix, type, minimumCount, description);

            return this;
        }

        public DecoratorBuilder WithFlags(FunctionFlags flags)
        {
            if (!Enum.IsDefined(typeof(FunctionFlags), flags))
            {
                // VisitMissingDeclarationSyntax in the TypeAssignmentVisitor uses the flags to determine the error message in cases of dangling decorators
                throw new ArgumentException($"The specified flags value is not explicitly defined in the {nameof(FunctionFlags)} enumeration. Define the combination and update usages to ensure the combination is handled correctly.");
            }

            this.functionOverloadBuilder.WithFlags(flags);

            return this;
        }

        public DecoratorBuilder WithAttachableType(TypeSymbol attachableType)
        {
            this.attachableType = attachableType;

            return this;
        }

        public DecoratorBuilder WithValidator(DecoratorValidator validator)
        {
            this.validator = validator;

            return this;
        }

        public DecoratorBuilder WithEvaluator(DecoratorEvaluator evaluator)
        {
            this.evaluator = evaluator;

            return this;
        }

        public Decorator Build() => new(this.functionOverloadBuilder.Build(), this.attachableType, this.validator, this.evaluator);
    }
}
