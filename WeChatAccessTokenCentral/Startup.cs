using System;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace WeChatAccessTokenCentral
{
	public class Startup
	{
		private static readonly HttpClient _client = new HttpClient();

		private record AccessTokenModel(string access_token, int expires_in);

		public void ConfigureServices(IServiceCollection services) {
			services.AddMemoryCache();
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
			app.UseDeveloperExceptionPage();
			app.UseRouting();

			app.UseEndpoints(endpoints => {
				endpoints.MapGet("/", async context => {

					var appId = context.Request.Query["appid"];
					var cache = app.ApplicationServices.GetRequiredService<IMemoryCache>();

					await context.Response.WriteAsync(
						await cache.GetOrCreateAsync(appId, async entry => {

							var url = "https://" + "api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&";
							var queryString = context.Request.QueryString.ToString().TrimStart('?', '&');

							var json = await _client.GetStringAsync(url + queryString);
							var result = JsonSerializer.Deserialize<AccessTokenModel>(json);
							var success = result != null && result.access_token != null;

							entry.SetAbsoluteExpiration(TimeSpan.FromMilliseconds(success ? (result.expires_in * 999) : 1));
							return json;
						})
					);
				});
			});
		}
	}
}
