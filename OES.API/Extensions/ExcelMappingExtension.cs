using OES.Helper.Dtos.Candidate.Requests;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
using OES.Services.Services;

namespace OES.API.Extensions
{
    public static class ExcelMappingExtension
    {
        public static IServiceCollection AddExcelMappingExtension(this IServiceCollection Services)
        {
            Services.AddTransient(typeof(IRowMapper), typeof(RowMapper));
            Services.AddTransient<DumpImportRowMapper>();
            Services.AddTransient(typeof(IFileProcessingValidator<AddMultipleCandidateDto>), typeof(AddMultipleCandidateDtoValidator));
            Services.AddTransient(typeof(IFileProcessingValidator<DumpImportCandidateRequestDto>), typeof(DumpImportCandidateRequestDtoValidator));

            Services.AddTransient<IFileProcessingService<AddMultipleCandidateDto>>(provider =>
            {
                var commonService = provider.GetRequiredService<ICommonService>();
                var rowMapper = provider.GetRequiredService<IRowMapper>();
                var validator = provider.GetRequiredService<IFileProcessingValidator<AddMultipleCandidateDto>>();

                Func<string[], string[], AddMultipleCandidateDto> mapRow = (headers, values) =>
                    rowMapper.MapRowToDto<AddMultipleCandidateDto>(headers, values);

                return new FileProcessingService<AddMultipleCandidateDto>(mapRow, commonService, validator);
            });

            Services.AddTransient<IFileProcessingService<DumpImportCandidateRequestDto>>(provider =>
            {
                var commonService = provider.GetRequiredService<ICommonService>();
                var specializedRowMapper = provider.GetRequiredService<DumpImportRowMapper>();
                var validator = provider.GetRequiredService<IFileProcessingValidator<DumpImportCandidateRequestDto>>();

                Func<string[], string[], DumpImportCandidateRequestDto> mapRow = (headers, values) =>
                    specializedRowMapper.MapRowToDto<DumpImportCandidateRequestDto>(headers, values);

                return new FileProcessingService<DumpImportCandidateRequestDto>(mapRow, commonService, validator);
            });

            Services.AddTransient<QuestionDeltaFileProcessingService>();

            return Services;
        }
    }
}