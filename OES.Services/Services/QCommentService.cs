using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Helper.Dtos.QcComments;
using OES.Helper.Dtos.QuestionComment;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.Interfaces;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using System.Net;

namespace OES.Services.Services
{
    public class QCommentService(ICommonService _commonService, IMapper _mapper) : IQCommentService
    {
        public async Task<IApiResponse> CreateComment(QCommentDto _commentDto)
        {
            if (string.IsNullOrEmpty(_commentDto.Comment))
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.InternalServerError,
                                                                  Resource.ErrorCreatingCommentpleaseentervalidcommentdetails);

            }

            var questionMetaData = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetObjAsync(x => x.Id == _commentDto.QuestionMetaDataId, Including: nameof(QuestionMetadata.SubQuestions));

            if (questionMetaData == null)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound,
                                                                  HttpStatusCode.NotFound,
                                                                  Resource.ThisQuestionDoesntexist);
            }

            var mappedComment = _mapper.Map<QCComment>(_commentDto);

            await _commonService._unitOfWork.Repository<QCComment, long>().AddAsync(mappedComment);

            questionMetaData.QuestionStatus = _commentDto.QCGivenStatus;

            foreach (var child in questionMetaData.SubQuestions ?? [])
            {
                child.QuestionStatus = _commentDto.QCGivenStatus;
            }

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success,
                                                                  HttpStatusCode.OK,
                                                                  Resource.CommentAddedSuccessfully);
            }
            else
            {
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                                                  HttpStatusCode.InternalServerError,
                                                                  Resource.FailToAddCommment);
            }
        }


        public async Task<IApiResponse> BypassQuestionsAsync(BypassQuestionsDto bypassQuestionsDto)
        {
            var questionMetaData = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(x => bypassQuestionsDto.QuestionMetaDataIds.Contains(x.Id));

            if (questionMetaData?.Any() != true)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.TheseQuestionsDontExist
                );
            }

            var allSubs = await _commonService
                ._unitOfWork
                .Repository<QuestionMetadata, long>()
                .GetAllAsync(x => questionMetaData.Select(q => q.Id).Contains(x.ParentId!.Value));

            var qcComments = new List<QCComment>();

            foreach (var question in questionMetaData)
            {
                question.QuestionStatus = QuestionStatus.Approved;

                var subs = allSubs.Where(x => x.ParentId == question.Id);

                foreach (var sub in subs)
                {
                    sub.QuestionStatus = QuestionStatus.Approved;
                }

                qcComments.Add(new QCComment
                {
                    Comment = MiscConstants.AutoBypassQuestionComment,
                    QCGivenStatus = nameof(QuestionStatus.Approved),
                    QCAuthor = bypassQuestionsDto.QCAuthor,
                    QuestionMetadataId = question.Id
                });
            }

            await _commonService._unitOfWork.Repository<QCComment, long>().AddRangeAsync(qcComments);

            if (await _commonService._unitOfWork.Complete() > 0)
            {
                return _commonService
                    ._apiResponse
                    .GetApiResponse(CustomCodeStatus.Success,
                                    HttpStatusCode.OK,
                                    $"{Resource.AllSelectedQuestionsHaveBeenApproved} ({questionMetaData.Count()})");
            }

            return _commonService
                ._apiResponse
                .GetApiResponse(CustomCodeStatus.SomethingWentWrong,
                                HttpStatusCode.BadRequest,
                                Resource.FailedToApproveTheSelectedQuestions);
        }


        public async Task<ApiResponse> GetQcCommentsByMetaDataId(PaginationSearchModel pagination, long metaDataId = 0)
        {
            var query = _commonService._unitOfWork.Repository<QCComment, long>().GetAll(e => e.QuestionMetadataId == metaDataId).AsNoTracking();

            if (!pagination.PaginationOff)
            {
                query = pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate)
                    : query.OrderBy(x => x.CreationDate);

                var totalItems = await query.CountAsync();

                var data = await query.Skip(pagination.PageIndex * pagination.PageSize).Take(pagination.PageSize).ToListAsync();

                var mappedData = _mapper.Map<List<QcCommentDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK, null,
                    new CustomTableData<QcCommentDto>(mappedData, totalItems)
                );
            }
            else
            {
                var data = await (pagination.OrderBy == SearchInKey.DESC
                    ? query.OrderByDescending(x => x.CreationDate).ToListAsync()
                    : query.OrderBy(x => x.CreationDate).ToListAsync());

                var mappedData = _mapper.Map<List<QcCommentDto>>(data);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK, null,
                    new CustomTableData<QcCommentDto>(mappedData, data.Count)
                );
            }
        }
    }
}
