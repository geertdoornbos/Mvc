// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Reads an object from the request body.
    /// </summary>
    public abstract class InputFormatter : IInputFormatter, IApiRequestFormatMetadataProvider
    {
        /// <summary>
        /// Gets the mutable collection of media type elements supported by
        /// this <see cref="InputFormatter"/>.
        /// </summary>
        public MediaTypeCollection SupportedMediaTypes { get; } = new MediaTypeCollection();

        /// <summary>
        /// Gets the default value for a given type. Used to return a default value when the body contains no content.
        /// </summary>
        /// <param name="modelType">The type of the value.</param>
        /// <returns>The default value for the <paramref name="modelType"/> type.</returns>
        protected virtual object GetDefaultValueForType(Type modelType)
        {
            if (modelType == null)
            {
                throw new ArgumentNullException(nameof(modelType));
            }

            if (modelType.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(modelType);
            }

            return null;
        }

        /// <inheritdoc />
        public virtual bool CanRead(InputFormatterContext context)
        {
            if (!CanReadType(context.ModelType))
            {
                return false;
            }

            var contentType = context.HttpContext.Request.ContentType;
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            // Confirm the request's content type is more specific than a media type this formatter supports e.g. OK if
            // client sent "text/plain" data and this formatter supports "text/*".
            return IsSubsetOfAnySupportedContentType(contentType);
        }

        private bool IsSubsetOfAnySupportedContentType(string contentType)
        {
            var parsedContentType = new MediaType(contentType);
            for (var i = 0; i < SupportedMediaTypes.Count; i++)
            {
                var supportedMediaType = new MediaType(SupportedMediaTypes[i]);
                if (parsedContentType.IsSubsetOf(supportedMediaType))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Determines whether this <see cref="InputFormatter"/> can deserialize an object of the given
        /// <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of object that will be read.</param>
        /// <returns><c>true</c> if the <paramref name="type"/> can be read, otherwise <c>false</c>.</returns>
        protected virtual bool CanReadType(Type type)
        {
            return true;
        }

        /// <inheritdoc />
        public virtual Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var request = context.HttpContext.Request;
            if (request.ContentLength == 0)
            {
                return InputFormatterResult.SuccessAsync(GetDefaultValueForType(context.ModelType));
            }

            return ReadRequestBodyAsync(context);
        }

        /// <summary>
        /// Reads an object from the request body.
        /// </summary>
        /// <param name="context">The <see cref="InputFormatterContext"/>.</param>
        /// <returns>A <see cref="Task"/> that on completion deserializes the request body.</returns>
        public abstract Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context);

        /// <inheritdoc />
        public IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        {
            if (!CanReadType(objectType))
            {
                return null;
            }

            if (contentType == null)
            {
                // If contentType is null, then any type we support is valid.
                return SupportedMediaTypes.Count > 0 ? SupportedMediaTypes : null;
            }
            else
            {
                var parsedContentType = new MediaType(contentType);
                List<string> mediaTypes = null;

                // Confirm this formatter supports a more specific media type than requested e.g. OK if "text/*"
                // requested and formatter supports "text/plain". Treat contentType like it came from an Content-Type header.
                foreach (var mediaType in SupportedMediaTypes)
                {
                    var parsedMediaType = new MediaType(mediaType);
                    if (parsedMediaType.IsSubsetOf(parsedContentType))
                    {
                        if (mediaTypes == null)
                        {
                            mediaTypes = new List<string>();
                        }

                        mediaTypes.Add(mediaType);
                    }
                }

                return mediaTypes;
            }
        }
    }
}