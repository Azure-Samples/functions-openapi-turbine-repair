using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace TurbineRepair
{
    public static class Turbines
    {
        const double revenuePerkW = 0.12;
        const double technicianCost = 250;
        const double turbineCost = 100;

        [FunctionName("TurbineRepair")]
        [OpenApiOperation(operationId: "Run")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "hours", In = ParameterLocation.Query, Type = typeof(Int32),
            Description = "Number of hours since turbine last serviced.")]
        [OpenApiParameter(name: "capacity", In = ParameterLocation.Query, Type = typeof(Int32),
            Description = "Kilowatt capacity of turbine.")]
        [OpenApiRequestBody("application/json", typeof(RequestBodyModel), 
            Description = "JSON request body containing { hours, capacity}")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string),
            Description = "The OK response message containing a JSON result.")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            // Get query strings if they exist
            int tempVal;
            int? hours = Int32.TryParse(req.Query["hours"], out tempVal) ? tempVal : (int?)null;
            int? capacity = Int32.TryParse(req.Query["capacity"], out tempVal) ? tempVal : (int?)null;

            // Get request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Use request body if a query was not sent
            capacity = capacity ?? data?.capacity;
            hours = hours ?? data?.hours;

            // Return bad request if capacity or hours are not passed in
            if (capacity == null || hours == null)
            {
                return new BadRequestObjectResult("Please pass capacity and hours on the query string or in the request body");
            }
            // Formulas to calculate revenue and cost
            double? revenueOpportunity = capacity * revenuePerkW * 24;
            double? costToFix = (hours * technicianCost) + turbineCost;
            string repairTurbine;

            if (revenueOpportunity > costToFix)
            {
                repairTurbine = "Yes";
            }
            else
            {
                repairTurbine = "No";
            };

            return (ActionResult)new OkObjectResult(new
            {
                message = repairTurbine,
                revenueOpportunity = "$" + revenueOpportunity,
                costToFix = "$" + costToFix
            });
        }
    }
    public class RequestBodyModel
    {
        public int Hours { get; set; }
        public int Capacity { get; set; } 
    }
}

