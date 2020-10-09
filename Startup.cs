using System;
using System.IO;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TimestampMicroservice
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();


                endpoints.MapGet("/", async context =>
                {
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "Home.html");
                    var bytes = File.ReadAllBytes(path);

                    context.Response.ContentType = MediaTypeNames.Text.Html;
                    await context.Response.StartAsync(context.RequestAborted);
                    await context.Response.BodyWriter.AsStream().WriteAsync(bytes);
                });

                endpoints.MapGet("/api/timestamp/{*dateString}", context =>
                {
                    var logger = context.RequestServices.GetRequiredService<ILogger<object>>();
                    if (!context.Request.RouteValues.TryGetValue("dateString", out object dateString))
                    {
                        var now = DateTimeOffset.UtcNow;
                        var responseObj = new
                        {
                            Unix = now.ToUnixTimeMilliseconds(),
                            UTC = now.ToString("r")
                        };
                        logger.LogInformation("Received empty date string " + responseObj);
                        SendResponse(context, responseObj, StatusCodes.Status200OK);
                    }
                    else
                    {
                        var date = (string)dateString;
                        if (DateTimeOffset.TryParse(date, out var parsedDate))
                        {
                            parsedDate = new DateTimeOffset(parsedDate.Date, TimeSpan.FromHours(0));
                            logger.LogInformation("Received date of " + parsedDate);
                            var epoc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                            var responseObj = new
                            {
                                // Unix = (parsedDate - epoc).TotalMilliseconds,
                                Unix = parsedDate.ToUnixTimeMilliseconds(),
                                UTC = parsedDate.ToString("r")
                            };

                            SendResponse(context, responseObj, StatusCodes.Status200OK);
                        }
                        else if (double.TryParse(date, out var epocMillis))
                        {
                            var baseEpocMilliSeconds = DateTime.UnixEpoch.Ticks / TimeSpan.TicksPerMillisecond;
                            var dateTime = new DateTime(TimeSpan.FromMilliseconds(baseEpocMilliSeconds + epocMillis).Ticks, DateTimeKind.Utc);
                            var responseObj = new
                            {
                                Unix = epocMillis,
                                UTC = dateTime.ToString("r")
                            };
                            SendResponse(context, responseObj, StatusCodes.Status200OK);
                        }
                        else
                        {
                            SendResponse(context, new { Error = "Invalid Date" }, StatusCodes.Status400BadRequest);
                        }
                    }

                    return System.Threading.Tasks.Task.CompletedTask;
                });
            });
        }

        private void SendResponse(HttpContext context, object responseObj, int statusCode, string contentType = MediaTypeNames.Application.Json)
        {
            var jsonOptions = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var response = JsonSerializer.Serialize(responseObj, jsonOptions);
            context.Response.ContentType = contentType;
            context.Response.StatusCode = statusCode;
            context.Response.WriteAsync(response);
        }
    }
}
