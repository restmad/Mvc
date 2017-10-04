// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiBehaviorApiDescriptionProvider : IApiDescriptionProvider
    {
        private readonly IModelMetadataProvider _modelMetadaProvider;

        public ApiBehaviorApiDescriptionProvider(IModelMetadataProvider modelMetadataProvider)
        {
            _modelMetadaProvider = modelMetadataProvider;
        }

        /// <remarks>
        /// The order is set to execute after the default provider.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
        }

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            foreach (var description in context.Results)
            {
                if (!AppliesTo(description))
                {
                    continue;
                }

                foreach (var responseType in CreateProblemResponseTypes(description))
                {
                    description.SupportedResponseTypes.Add(responseType);
                }
            }
        }

        public bool AppliesTo(ApiDescription description)
        {
            return description.ActionDescriptor.FilterDescriptors.Any(f => f.Filter is IApiBehaviorMetadata);
        }

        public IEnumerable<ApiResponseType> CreateProblemResponseTypes(ApiDescription description)
        {
            if (description.ActionDescriptor.Parameters.Any() || description.ActionDescriptor.BoundProperties.Any())
            {
                // For validation errors.
                yield return CreateProblemResponse(StatusCodes.Status400BadRequest);

                if (description.ActionDescriptor.Parameters.Any(p => p.Name.EndsWith("id", StringComparison.OrdinalIgnoreCase)))
                {
                    yield return CreateProblemResponse(StatusCodes.Status404NotFound);
                }
            }

            yield return CreateProblemResponse(statusCode: 0, isDefaultResponse: true);
        }
        
        private ApiResponseType CreateProblemResponse(int statusCode, bool isDefaultResponse = false)
        {
            return new ApiResponseType
            {
                ApiResponseFormats = new List<ApiResponseFormat>
                {
                    new ApiResponseFormat
                    {
                        MediaType = "application/problem+json",
                    },
                    new ApiResponseFormat
                    {
                        MediaType = "application/problem+xml",
                    },
                },
                IsDefaultResponse = isDefaultResponse,
                ModelMetadata = _modelMetadaProvider.GetMetadataForType(typeof(ProblemDetails)),
                StatusCode = statusCode,
                Type = typeof(ProblemDetails),
            };
        }
    }
}
