using AutoMapper;
using OES.Helper.Interfaces;
using OES.Interface.Interfaces;
using OES.Interface.UnitOfWork;

namespace OES.Services.Services
{
    public class CommonService : ICommonService
    {
        public IUnitOfWork _unitOfWork { get; set; }
        public IApiResponse _apiResponse { get; set; }
        public IMapper _mapper { get; }

        public CommonService(IUnitOfWork unitOfWork, IApiResponse apiResponse, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _apiResponse = apiResponse;
            _mapper = mapper;
        }
    }
}
