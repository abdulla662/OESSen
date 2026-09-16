using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OES.Core.Entities;
using OES.Core.Entities.QuestionQualityCheck;
using OES.Helper.Dtos.ItemBank;
using OES.Helper.Dtos.QualityCheckCommittee;
using OES.Helper.Enums;
using OES.Helper.General;
using OES.Helper.ResourceFiles;
using OES.Interface.Interfaces;
using SharedHelper.General;
using System.Net;

namespace OES.Services.Services
{
    public class QualityCheckCommitteeService : IQualityCheckCommitteeService
    {
        private readonly ICommonService _commonService;
        private readonly IMapper _mapper;
        private readonly FilterParamsValues _filterParamsValues;


        public QualityCheckCommitteeService(ICommonService commonService, IMapper mapper, FilterParamsValues filterParamsValues)
        {
            _commonService = commonService;
            _mapper = mapper;
            _filterParamsValues = filterParamsValues;
        }


        public async Task<ApiResponse> GetAllQualityCheckCommitteePaginatedListAsync(PaginationSearchModel pagination)
        {
            var query = _commonService
                ._unitOfWork
                .Repository<QualityCheckCommittee, long>()
                .GetAll(Including: $"{nameof(QualityCheckCommittee.Chief)},{nameof(QualityCheckCommittee.Members)}.{nameof(QualityCheckCommitteeMember.User)}")
                .AsNoTracking();

            if (!string.IsNullOrEmpty(pagination.SearchKey))
            {
                if (pagination.SearchInName && pagination.SearchInDescription)
                {
                    query = query.Where(c => c.Name.Contains(pagination.SearchKey) || c.Description.Contains(pagination.SearchKey));
                }
                else if (pagination.SearchInName)
                {
                    query = query.Where(c => c.Name.Contains(pagination.SearchKey));
                }
                else if (pagination.SearchInDescription)
                {
                    query = query.Where(c => c.Description.Contains(pagination.SearchKey));
                }
            }

            if (pagination.FromDate is not null)
            {
                query = query.Where(c => c.CreationDate >= pagination.FromDate && c.CreationDate < (pagination.ToDate ?? DateTimeHelper.Now.Date).AddDays(1));
            }

            query = pagination.OrderBy == SearchInKey.DESC
                ? query.OrderByDescending(c => c.CreationDate)
                : query.OrderBy(c => c.CreationDate);

            if (!pagination.PaginationOff)
            {
                var totalItems = await query.CountAsync();

                var committees = await query
                    .Skip(pagination.PageIndex * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                var mapped = _mapper.Map<List<QualityCheckCommitteeDto>>(committees);

                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Success,
                    HttpStatusCode.OK,
                    Resource.QuestionsQualityCheckCommitteesFetchedSuccessfullyPaginated,
                    new CustomTableData<QualityCheckCommitteeDto>(mapped, totalItems)
                );
            }

            var allCommittees = await query.ToListAsync();

            var mappedAll = _mapper.Map<List<QualityCheckCommitteeDto>>(allCommittees);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.QuestionsQualityCheckCommitteesFetchedSuccessfullyNoPagination,
                new CustomTableData<QualityCheckCommitteeDto>(mappedAll, allCommittees.Count)
            );
        }

        public async Task<ApiResponse> GetAllQualityCheckCommitteeAsync()
        {
            var committees = await _commonService
                ._unitOfWork
                .Repository<QualityCheckCommittee, long>()
                .GetAll(Including: $"{nameof(QualityCheckCommittee.Chief)},{nameof(QualityCheckCommittee.Members)}.{nameof(QualityCheckCommitteeMember.User)}")
                .AsNoTracking()
                .ToListAsync();

            var mapped = _mapper.Map<List<QualityCheckCommitteeDto>>(committees);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                mapped
            );
        }

        public async Task<ApiResponse> GetQualityCheckCommitteeByIdAsync(long id)
        {
            var committee = await _commonService
                ._unitOfWork
                .Repository<QualityCheckCommittee, long>()
                .Query()
                .Include(c => c.Chief)
                .Include(c => c.Members)
                .ThenInclude(m => m.User)
                .Include(c => c.QualityCheckCommitteeItemBanks)
                .ThenInclude(qci => qci.ItemBank)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (committee == null)
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.CommitteeNotFound);

            var mapped = _mapper.Map<QualityCheckCommitteeDto>(committee);

            mapped.ItemBanks = [.. committee.QualityCheckCommitteeItemBanks
                .Select(link => new QualityCheckItemBankDto
                {
                    Id = link.ItemBank.Id,
                    Name = link.ItemBank.Name
                })
            ];

            if (committee.QualityCheckCommitteeItemBanks.Count > 0)
            {
                var firstItemBankId = committee.QualityCheckCommitteeItemBanks.First().ItemBankId;

                var root = await FindRootForItemBankAsync(firstItemBankId);

                if (root != null)
                {
                    mapped.SelectedRootItemBank = new RootItemBankDto
                    {
                        Id = root.Id,
                        Name = root.Name,
                        OrganizationSignature = root.OrganizationSignature
                    };
                }
            }

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                mapped
            );
        }

        public async Task<ApiResponse> GetQualityCheckCommitteeMembersAsync(long committeeId)
        {
            var members = await _commonService
                ._unitOfWork
                .Repository<QualityCheckCommitteeMember, long>()
                .GetAllAsync(m => m.CommitteeId == committeeId, Including: nameof(QualityCheckCommitteeMember.User), asNoTracking: true);

            if (!members.Any())
                return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.NotFound, HttpStatusCode.NotFound, Resource.NoMembersFound);

            var mapped = members.Select(m => new QualityCheckCommitteeMemberDto
            {
                UserId = m.UserId,
                Username = m.User.Username,
                Email = m.User.EmailAddress,
                IsActive = m.IsActive
            })
            .ToList();

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                null,
                mapped
            );
        }

        public async Task<ApiResponse> CreateCommitteeWithMembersAsync(CreateQualityCheckCommitteeRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.CommitteeNameIsrequired
                );
            }

            var memberIds = dto.MemberUserIds.Distinct().ToList();

            if (memberIds.Count == 0)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.AtLeastOneMemberIsRequired
                );
            }

            if (!memberIds.Contains(dto.ChiefUserId))
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.ChiefUserMustAlsoBeIncludedInTheCommitteeMembers
                );
            }

            var committeeRepo = _commonService._unitOfWork.Repository<QualityCheckCommittee, long>();

            var nameExists = await committeeRepo.IsExistAsync(c => c.Name.Trim().ToLower() == dto.Name.Trim().ToLower());

            if (nameExists)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.Conflict,
                    HttpStatusCode.Conflict,
                    Resource.ACommitteeWithTheSameNameAlreadyExists
                );
            }

            var committee = new QualityCheckCommittee
            {
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                ChiefId = dto.ChiefUserId
            };

            await committeeRepo.AddAsync(committee);

            var memberRepo = _commonService._unitOfWork.Repository<QualityCheckCommitteeMember, long>();

            var members = memberIds.ConvertAll(uid => new QualityCheckCommitteeMember
            {
                Committee = committee,
                UserId = uid
            });

            memberRepo.AddRangAsync(members);

            if (dto.SelectedItemBankIds != null)
            {
                var committeeItemBankRepo = _commonService._unitOfWork.Repository<QualityCheckCommitteeItemBank, long>();

                var itemBankLinks = dto.SelectedItemBankIds.ConvertAll(itemBankId => new QualityCheckCommitteeItemBank
                {
                    Committee = committee,
                    ItemBankId = itemBankId
                });

                committeeItemBankRepo.AddRangAsync(itemBankLinks);
            }

            await _commonService._unitOfWork.Complete();

            var createdDto = new QualityCheckCommitteeCreatedDto
            {
                CommitteeId = committee.Id,
                Name = committee.Name
            };

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CommitteeCreatedWithMembersAndChiefAssignedSuccessfully,
                createdDto
            );
        }

        public async Task<ApiResponse> EditQualityCheckCommitteeByIdAsync(QualityCheckCommitteeDto dto)
        {
            if (dto == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.ValidationError,
                    HttpStatusCode.BadRequest,
                    Resource.InputDataCannotBeNull
                );
            }

            var repo = _commonService._unitOfWork.Repository<QualityCheckCommittee, long>();

            var committee = await repo
                .GetObjAsync(c => c.Id == dto.Id, Including: $"{nameof(QualityCheckCommittee.Members)},{nameof(QualityCheckCommittee.QualityCheckCommitteeItemBanks)}");

            if (committee == null)
            {
                return _commonService._apiResponse.GetApiResponse(
                    CustomCodeStatus.NotFound,
                    HttpStatusCode.NotFound,
                    Resource.CommitteeNotFound
                );
            }

            committee.Name = dto.Name.Trim();
            committee.Description = dto.Description.Trim();
            committee.ChiefId = dto.ChiefId;

            repo.Update(committee);

            var memberRepo = _commonService._unitOfWork.Repository<QualityCheckCommitteeMember, long>();

            var committeeItemBankRepo = _commonService._unitOfWork.Repository<QualityCheckCommitteeItemBank, long>();

            if (committee.Members.Count > 0)
            {
                memberRepo.DeleteRange([.. committee.Members]);
            }
            if (committee.QualityCheckCommitteeItemBanks.Count > 0)
            {
                committeeItemBankRepo.DeleteRange([.. committee.QualityCheckCommitteeItemBanks]);
            }

            if (dto.Members?.Count > 0)
            {
                var newMembers = dto
                    .Members
                    .DistinctBy(m => m.UserId)
                    .Select(m => new QualityCheckCommitteeMember
                    {
                        CommitteeId = committee.Id,
                        UserId = m.UserId,
                        IsActive = m.IsActive
                    }).ToList();

                memberRepo.AddRangAsync(newMembers);
            }

            if (dto.ItemBanks?.Count > 0)
            {
                var newItemBanks = dto.ItemBanks
                    .ConvertAll(ib => new QualityCheckCommitteeItemBank
                    {
                        QualityCheckCommitteeId = committee.Id,
                        ItemBankId = ib.Id
                    });

                committeeItemBankRepo.AddRangAsync(newItemBanks);
            }

            await _commonService._unitOfWork.Complete();

            var mappedDto = _mapper.Map<QualityCheckCommitteeDto>(committee);

            return _commonService._apiResponse.GetApiResponse(
                CustomCodeStatus.Success,
                HttpStatusCode.OK,
                Resource.CommitteeUpdatedSuccessfully,
                mappedDto
            );
        }

        //public async Task<ApiResponse> GetUserQualityCheckCommitteeGroupsAsync()
        //{
        //    var currentUser = _filterParamsValues.UserEmail?.Trim().ToLowerInvariant() ?? "system";
        //    var groupRepo = _commonService._unitOfWork.Repository<OESGroup, Guid>();

        //    var groups = await groupRepo.GetAllAsync(g =>
        //        !g.IsDeleted &&
        //        !g.IsTemplate &&
        //        !g.IsPredefined &&
        //        !g.AutoCreatedForUser &&
        //        g.GroupResources.Any(r => !r.IsDeleted && r.ResourceType == ResourceType.QualityCheckCommittee) &&
        //        (
        //            g.CreationUser.ToLower() == currentUser ||
        //            g.GroupResources.Any(r => r.CreationUser.ToLower() == currentUser)
        //        ),
        //        Including: nameof(OESGroup.GroupResources)
        //    );

        //    var groupDtos = groups
        //        .Select(g => new GetOESGroupDto { Id = g.Id, Name = g.Name })
        //        .DistinctBy(x => x.Id)
        //        .OrderBy(x => x.Name)
        //        .ToList();

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, groupDtos);
        //}

        //public async Task<ApiResponse> GetQualityCheckCommitteeGroupsAsync(long qualityCheckCommitteeId)
        //{
        //    var groups = await _commonService._unitOfWork
        //        .Repository<QualityCheckCommitteeGroups, long>()
        //        .GetAllAsync(
        //            x => x.QualityCheckCommitteeId == qualityCheckCommitteeId && !x.OESGroup.IsTemplate,
        //            Including: nameof(QualityCheckCommitteeGroups.OESGroup)
        //        );

        //    var dto = new QualityCheckCommitteeGroupDto();
        //    groups.ToList().ForEach(g => dto.GroupsIds.Add(g.OESGroupId));
        //    dto.OwnerGroupId = groups.FirstOrDefault(x => x.OESGroup?.AutoCreatedForUser == true)?.OESGroupId;

        //    return _commonService._apiResponse.GetApiResponse(CustomCodeStatus.Success, HttpStatusCode.OK, Resource.SuccessfulFetching, dto);
        //}


        #region Helper Methods

        private async Task<ItemBank> FindRootForItemBankAsync(long itemBankId)
        {
            var itemBankRepo = _commonService._unitOfWork.Repository<ItemBank, long>();

            var currentItem = await itemBankRepo.GetByIdAsync(itemBankId);

            while (currentItem?.ParentId != null)
            {
                currentItem = await itemBankRepo.GetByIdAsync(currentItem.ParentId.Value);
            }

            return currentItem;
        }

        #endregion
    }
}
