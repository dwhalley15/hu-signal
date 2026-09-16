using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using Umbraco.Cms.Api.Common.OpenApi;
using Umbraco.Cms.Api.Management.OpenApi;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace HuSignal.Composers
{
    public class HuSignalApiComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services
                .AddOptions<HuSignalSettings>()
                .Bind(builder.Config.GetSection(HuSignalSettings.SectionName));

            builder.Services.AddHttpClient<IClarityService, ClarityService>();

            builder.Services.AddScoped<IClarityRepository, ClarityRepository>();

            builder.Services.AddScoped<IClarityImportService, ClarityImportService>();

            builder.Services.AddScoped<IClarityReportingService, ClarityReportingService>();

            builder.Services.AddSingleton<IOperationIdHandler, HuSignalOperationIdHandler>();

            builder.Services.AddHostedService<ClarityImportBackgroundService>();

            builder.Services.Configure<SwaggerGenOptions>(options =>
            {
                options.SwaggerDoc(Constants.ApiName, new OpenApiInfo
                {
                    Title = "Hu Signal Backoffice API",
                    Version = "1.0"
                });

                options.OperationFilter<HuSignalOperationSecurityFilter>();
            });
        }

        public class HuSignalOperationSecurityFilter
            : BackOfficeSecurityRequirementsOperationFilterBase
        {
            protected override string ApiName => Constants.ApiName;
        }

        public class HuSignalOperationIdHandler : OperationIdHandler
        {
            public HuSignalOperationIdHandler(
                IOptions<ApiVersioningOptions> apiVersioningOptions)
                : base(apiVersioningOptions)
            {
            }

            protected override bool CanHandle(
                ApiDescription apiDescription,
                ControllerActionDescriptor controllerActionDescriptor)
            {
                return controllerActionDescriptor.ControllerTypeInfo.Namespace?
                    .StartsWith(
                        "HuSignal.Controllers",
                        StringComparison.InvariantCultureIgnoreCase)
                    is true;
            }

            public override string Handle(ApiDescription apiDescription)
            {
                return apiDescription.ActionDescriptor.RouteValues["action"]!;
            }
        }
    }
}