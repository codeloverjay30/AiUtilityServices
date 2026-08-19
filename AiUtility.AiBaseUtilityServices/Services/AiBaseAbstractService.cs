using AiUtility.Configurations;
using ExceptionHandlingUtilityServices;
using FluentValidation;
using LoggerFactoryUtilityServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Services
{
    public abstract class AiBaseAbstractService(
        ILoggerFactoryBaseUtilityService loggerFactoryService ,
        bool toLogWhenSuccess
      ): ExceptionHandler(
        loggerFactoryService,
        toLogWhenSuccess
    )
    {
        protected readonly ILoggerFactoryBaseUtilityService _loggerFactoryService = loggerFactoryService;
        public ILoggerFactoryBaseUtilityService LoggerFactoryService => _loggerFactoryService;
        protected readonly bool _toLogWhenSuccess = toLogWhenSuccess;

        /// <summary>
        /// Validate the request model
        /// </summary>
        /// <typeparam name="T">type of request model</typeparam>
        /// <param name="request"><see cref="AiUtility.GeminiUtilityServices.Models.GeminiGenerateRequest"/></param>
        /// <param name="validator">validator</param>
        /// <returns></returns>
        /// <exception cref="FluentValidation.ValidationException"></exception>

        protected async Task ValidateRequestAsync<T>(T request , IValidator<T> validator)
        {
            var result = await validator.ValidateAsync(request);

            if(!result.IsValid)
            {
                var errorDetails = string.Join(" | " , result.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
                throw new FluentValidation.ValidationException(result.Errors);
            }
        }
    }
}
