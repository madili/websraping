using System.IO.Compression;
using Ben.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerUI;
using WebScraping.Services;

namespace WebScraping
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
            services.AddTransient<ScrapingService>();

            services.AddHttpClient();

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

            services.AddResponseCompression(options => options.Providers.Add<GzipCompressionProvider>());

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            ConfigureSwagger(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseBlockingDetection();

            app.UseResponseCompression();

            app.UseSwagger(options =>
            {
                options.PreSerializeFilters.Add((document, request) =>
                {
                    document.Paths = document.Paths;//document.Paths.ToDictionary(p => p.Key.ToLowerInvariant(), p => p.Value);
                });
            });

            app.UseSwaggerUI(configuration =>
            {
                configuration.SwaggerEndpoint($"/swagger/v1/swagger.json", "Webscraping API");
                configuration.DocExpansion(DocExpansion.None);
            });

            app.UseHttpsRedirection();
            app.UseMvc();
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();
                options.SwaggerDoc("v1",
                    new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        Title = "WebScraping",
                        Description = "API that returns the total number of lines and the total number of bytes of all the files of a given public Github repository, grouped by file extension.",
                        Version = "v1"
                    });
            });

        }
    }
}
