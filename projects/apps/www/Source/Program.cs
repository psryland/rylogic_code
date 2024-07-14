using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using rylogic.co.nz;

// Create the app builder
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Create the app instance
var app = builder.Build();

// Load app config
var config = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddJsonFileIf(app.Environment.IsDevelopment(), "appsettings.Development.json", optional: true)
	.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true)
{
	app.UseDeveloperExceptionPage();
}
else
{
	app.UseExceptionHandler("/Error");
	app.UseHsts(); // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}

// Https always
app.UseHttpsRedirection();

// Setup the file store root path
var file_store_path = config[ConfigTag.FileStore] ?? throw new Exception("FileStore path not set");
if (!Directory.Exists(file_store_path))
	throw new Exception($"FileStore path does not exist: ${file_store_path}");

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
	RequestPath = Paths.FileStore,
	FileProvider = new PhysicalFileProvider(file_store_path),
	ContentTypeProvider = new FileExtensionContentTypeProvider
	{
		Mappings =
		{
			[".apk"] = "application/vnd.android.package-archive",
			[".vsix"] = "application/octet-stream",
		}
	},
});

// Set the default landing page
app.UseDefaultFiles(new DefaultFilesOptions { DefaultFileNames = ["Index.cshtml"] });

app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
