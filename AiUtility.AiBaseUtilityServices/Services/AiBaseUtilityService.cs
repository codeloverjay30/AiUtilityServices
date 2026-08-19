using ExceptionHandlingUtilityServices;
using LoggerFactoryUtilityServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace AiUtility.AiBaseUtilityServices.Services
{
    public class AiBaseUtilityService (
        ILoggerFactoryBaseUtilityService loggerFactoryService ,
        bool toLogWhenSuccess
    ) : AiBaseAbstractService(
           loggerFactoryService ,
           toLogWhenSuccess
    )
    {
    }
}
