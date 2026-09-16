using AutoMapper;
using OES.Helper.Interfaces;
using OES.Interface.UnitOfWork;

namespace OES.Interface.Interfaces
{
    public interface ICommonService
    {
        IUnitOfWork _unitOfWork { set; get; }

        IApiResponse _apiResponse { set; get; }

        IMapper _mapper { get; }
    }
}
