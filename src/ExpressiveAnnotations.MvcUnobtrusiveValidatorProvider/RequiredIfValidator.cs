﻿using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web.Mvc;
using ExpressiveAnnotations.ConditionalAttributes;
using System.Collections.Generic;
using ExpressiveAnnotations.Misc;
using Newtonsoft.Json;

namespace ExpressiveAnnotations.MvcUnobtrusiveValidatorProvider
{
    /// <summary>
    /// Model validator for <see cref="RequiredIfAttribute"/>.
    /// </summary>
    public class RequiredIfValidator : DataAnnotationsModelValidator<RequiredIfAttribute>
    {
        private readonly string _errorMessage;
        private readonly string _dependentProperty;
        private readonly string _relationalOperator;
        private readonly object _targetValue;
        private readonly string _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequiredIfValidator"/> class.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <param name="context">The context.</param>
        /// <param name="attribute">The attribute.</param>
        public RequiredIfValidator(ModelMetadata metadata, ControllerContext context, RequiredIfAttribute attribute)
            : base(metadata, context, attribute)
        {
            var dependentProperty = MiscHelper.ExtractProperty(metadata.ContainerType, attribute.DependentProperty);
            var relationalOperator = attribute.RelationalOperator ?? "==";

            string targetPropertyName;
            var attributeName = GetType().BaseType.GetGenericArguments().Single().Name;
            if (MiscHelper.TryExtractName(attribute.TargetValue, out targetPropertyName))
            {
                var targetProperty = MiscHelper.ExtractProperty(metadata.ContainerType, targetPropertyName);
                Assert.ConsistentTypes(dependentProperty, targetProperty, metadata.PropertyName, attributeName, relationalOperator);
            }
            else
                Assert.ConsistentTypes(dependentProperty, attribute.TargetValue, metadata.PropertyName, attributeName, relationalOperator);

            var displayAttribute = dependentProperty.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            var dependentPropertyName = displayAttribute != null ? displayAttribute.GetName() : attribute.DependentProperty;
            
            _dependentProperty = attribute.DependentProperty;
            _relationalOperator = relationalOperator;
            _targetValue = attribute.TargetValue;

            _type = TypeHelper.GetCoarseType(dependentProperty.PropertyType);
            _errorMessage = attribute.FormatErrorMessage(metadata.GetDisplayName(), dependentPropertyName);
        }

        /// <summary>
        /// Retrieves a collection of client validation rules (rules sent to browsers).
        /// </summary>
        public override IEnumerable<ModelClientValidationRule> GetClientValidationRules()
        {
            var rule = new ModelClientValidationRule
            {
                ErrorMessage = _errorMessage,
                ValidationType = "requiredif",
            };
            rule.ValidationParameters.Add("dependentproperty", JsonConvert.SerializeObject(_dependentProperty));
            rule.ValidationParameters.Add("relationaloperator", JsonConvert.SerializeObject(_relationalOperator));
            rule.ValidationParameters.Add("targetvalue", JsonConvert.SerializeObject(_targetValue));
            rule.ValidationParameters.Add("type", JsonConvert.SerializeObject(_type));
            yield return rule;
        }
    }
}
