using AutoMapper;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using OES.Api.Protos;
using OES.Helper.Dtos.AppUserProfileDtos;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
using SharedHelper.Enums;
using SharedHelper.General;
using System.Net;

namespace OES.API.GRPCService
{
    public class UserProfileGRPCService : IUserProfileGRPCService
    {
        private readonly IMapper _mapper;
        private readonly IApiResponse _apiResponse;

        public UserProfileGRPCService(IMapper mapper, IApiResponse apiResponse)
        {
            _mapper = mapper;
            _apiResponse = apiResponse;
        }

        public async Task<ApiResponse> GetAllOrganizationUserProfile(UserParamsForSync userParamsForSync)
        {
            var handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            using var channel = GrpcChannel.ForAddress(CentralizedUrlHelper.SsoApiBaseUrl, new GrpcChannelOptions
            {
                HttpHandler = handler,
                MaxReceiveMessageSize = int.MaxValue,
                MaxSendMessageSize = int.MaxValue
            });

            var client = new UserProfile.UserProfileClient(channel);

            var request = new UserProfileRequest
            {
                OrganizationSignature = userParamsForSync.OrganizationSignature,
                ModuleId = userParamsForSync.ModuleId,
                OrganizationId = userParamsForSync.OrganizationId
            };

            var response = await client.GetUserProfileAsync(request);

            // Directly map to DTOs from the gRPC response:
            var userProfilesDtos = response.Profiles.Select(profile =>
            {
                return new AppUserProfileRetrievalDto
                {
                    Id = Guid.Parse(profile.Id),
                    UserName = profile.Username,
                    EmailAddress = profile.EmailAddress,
                    CreationUser = profile.CreationUser,
                    ModeficationUser = profile.ModificationUser,
                    CreationDate = profile.CreationDate.ToDateTime(),
                    ModeficationDate = profile.ModificationDate.ToDateTime(),
                    IsDeleted = profile.IsDeleted,
                    DeletedDate = profile.DeletedDate?.ToDateTime(),
                    IsActive = profile.IsActive,
                    OrganizationSignature = profile.OrganizationSignature,
                    ModuleUserID = Guid.Parse(profile.ModuleUserID),
                    OrganizationId = profile.OrganizationId,
                    Gender = (Gender?)profile.Gender,
                };
            }).ToList();

            return _apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                               HttpStatusCode.OK,
                                               null!,
                                               userProfilesDtos);
        }
    }
}
