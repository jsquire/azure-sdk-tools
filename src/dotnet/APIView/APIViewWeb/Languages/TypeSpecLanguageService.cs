// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading.Tasks;
using ApiView;
using APIViewWeb.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace APIViewWeb
{
    public class TypeSpecLanguageService : LanguageProcessor
    {
        private readonly TelemetryClient _telemetryClient;
        private string _typeSpecSpecificPathPrefix;
        private readonly string _reviewGenerationPipelineUrl;

        public override string Name { get; } = "TypeSpec";
        public override string [] Extensions { get; } = { ".tsp", ".cadl" };
        public override string VersionString { get; } = "0.5.0";
        public override string ReviewGenerationPipelineUrl => _reviewGenerationPipelineUrl;

        public override string ProcessName => throw new NotImplementedException();

        public TypeSpecLanguageService(IConfiguration configuration, TelemetryClient telemetryClient) : base(telemetryClient)
        {
            IsReviewGenByPipeline = true;
            _typeSpecSpecificPathPrefix = "/specification";
            _reviewGenerationPipelineUrl = configuration["TypeSpecReviewGenerationPipelineUrl"] ?? String.Empty;
            _telemetryClient = telemetryClient;
        }
        public override async Task<CodeFile> GetCodeFileAsync(string originalName, Stream stream, bool runAnalysis)
        {
            return await CodeFile.DeserializeAsync(stream);
        }

        public override string GetProcessorArguments(string originalName, string tempDirectory, string jsonPath)
        {
            throw new NotImplementedException();
        }
        public override bool CanUpdate(string versionString)
        {
            return false;
        }

        public override bool CanConvert(string versionString)
        {
            return false;
        }

        public override bool GeneratePipelineRunParams(APIRevisionGenerationPipelineParamModel param)
        {
            var filePath = param.FileName;
            // Verify TypeSpec source file path is a GitHub URL to TypeSpec package root 
            if (filePath == null || !filePath.StartsWith("https://github.com/"))
                return false;
          
            if (!filePath.Contains("/tree/") || !filePath.Contains(_typeSpecSpecificPathPrefix))
                return false;

            filePath = filePath.Replace("https://github.com/", "");
            var sourceUrlparts = filePath.Split("/tree/", 2);
            param.SourceRepoName = sourceUrlparts[0];
            sourceUrlparts = sourceUrlparts[1].Split(_typeSpecSpecificPathPrefix, 2);
            param.SourceBranchName = sourceUrlparts[0];
            param.FileName = $"{_typeSpecSpecificPathPrefix}{sourceUrlparts[1]}";
            _telemetryClient.TrackTrace($"Pipeline parameters to run TypeSpec API rview gen pipeline: '{JsonConvert.SerializeObject(param)}'");

            return true;
        }
    }
}
