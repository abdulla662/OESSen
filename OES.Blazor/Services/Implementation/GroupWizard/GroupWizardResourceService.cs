using MudBlazor;
using OES.Blazor.Models.RuleMatrix;
using OES.Blazor.Services.Interfaces.Group;
using OES.Blazor.Services.Interfaces.GroupWizard;
using OES.Blazor.Services.Interfaces.ILOSection;
using OES.Blazor.Services.Interfaces.ItemBank;
using OES.Blazor.Services.Interfaces.Paper;
using OES.Blazor.Services.Interfaces.Question;
using OES.Blazor.Services.Interfaces.Schedule;
using OES.Helper.Dtos.ILO;
using OES.Helper.Dtos.OESUserGroups;
using OES.Helper.Dtos.Schedule.Requestes;
using OES.Helper.Enums;
using OES.Helper.General;
using System.Text.Json;

namespace OES.Blazor.Services.Implementation.GroupWizard
{
    public class GroupWizardResourceService(
        IBlazScheduleService schedule,
        IBlazPaperService paper,
        IBlazItemBankService itemBank,
        IBlazILOService ilo,
        IBlazQuestionService question,
        IBlazGroupService groupService) : IGroupWizardResourceService
    {
        public bool IsTreeResource(ResourceType r) =>
            r is ResourceType.Ilo or ResourceType.ItemBank;

        public bool IsConfigOnlyResource(ResourceType r) =>
            r is ResourceType.Configurations or ResourceType.FileManger
              or ResourceType.Candidate or ResourceType.Report or ResourceType.Result;

        // ── Flat resources ───────────────────────────────────────────────────

        public async Task<TableData<ResourceInstanceItem>> FetchPageAsync(
            ResourceType r,
            TableState state,
            string searchKey
        )
        {
            var p = new PaginationSearchModel
            {
                PageIndex = state.Page,
                PageSize = state.PageSize,
                PaginationOff = false,
                SearchKey = string.IsNullOrWhiteSpace(searchKey) ? null : searchKey,
                SearchInName = true,
                SearchInDescription = true
            };

            return r switch
            {
                ResourceType.Questions => await FetchQuestionsAsync(p),
                ResourceType.Papers => await FetchPapersAsync(p),
                ResourceType.Schedule => await FetchScheduleAsync(p),
                _ => new TableData<ResourceInstanceItem> { Items = [], TotalItems = 0 }
            };
        }

        // ── Tree resources ───────────────────────────────────────────────────

        public async Task<TableData<ResourceInstanceItem>> LoadRootsPageAsync(
            ResourceType r,
            int page,
            int pageSize,
            string? searchKey = null
        )
        {
            var p = new PaginationSearchModel
            {
                PaginationOff = false,
                PageIndex = page,
                PageSize = pageSize,
                SearchKey = string.IsNullOrWhiteSpace(searchKey) ? null : searchKey,
                SearchInName = true,
                SearchInDescription = true
            };

            if (r == ResourceType.ItemBank)
            {
                var data = await itemBank.GetAllItemBankAsync(p);
                return new TableData<ResourceInstanceItem>
                {
                    TotalItems = data?.TotalItems ?? 0,
                    Items = data?.Items?.Select(x => new ResourceInstanceItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SubLabel = x.Code,
                        Signature = x.ItemBankSignature,
                        IsLeaf = false
                    }).ToList() ?? []
                };
            }

            if (r == ResourceType.Ilo)
            {
                var data = await ilo.GetAllRoots(p);
                return new TableData<ResourceInstanceItem>
                {
                    TotalItems = data?.TotalItems ?? 0,
                    Items = data?.Items?.Select(x => new ResourceInstanceItem
                    {
                        Id = x.Id,
                        Name = x.Name,
                        SubLabel = x.Code,
                        Signature = x.signature,
                        IsLeaf = false
                    }).ToList() ?? []
                };
            }

            return new TableData<ResourceInstanceItem> { Items = [], TotalItems = 0 };
        }

        public async Task<List<ResourceInstanceItem>> LoadChildrenAsync(
            ResourceType r,
            long parentId,
            string? signature = null
        )
        {
            if (r == ResourceType.ItemBank)
            {
                var children = await itemBank.GetChildrenByParentIdAsync(parentId, signature ?? "");
                return children.ConvertAll(c => new ResourceInstanceItem
                {
                    Id = c.Id,
                    Name = c.Text,
                    SubLabel = c.Code,
                    Signature = c.Signature,
                    IsLeaf = false
                });
            }

            if (r == ResourceType.Ilo)
            {
                var children = await ilo.GetChildrenByParentIdAsync(parentId, signature ?? "");
                return children.ConvertAll(c => new ResourceInstanceItem
                {
                    Id = c.Id,
                    Name = c.Text,
                    SubLabel = c.Code,
                    Signature = c.Signature,
                    IsLeaf = false
                });
            }

            return [];
        }

        // ── Assignment ───────────────────────────────────────────────────────

        public async Task<List<long>> GetAssignedItemIdsAsync(ResourceType resourceType, Guid groupId)
        {
            if (IsConfigOnlyResource(resourceType)) return [];
            return await groupService.GetAssignedEntityIdsAsync(groupId, resourceType);
        }

        public async Task AssignGroupToItemsAsync(ResourceType r, Guid groupId, List<long> ids)
        {
            switch (r)
            {
                case ResourceType.Schedule:
                    var prevSchedule = await GetAssignedItemIdsAsync(ResourceType.Schedule, groupId);
                    foreach (var id in ids.Union(prevSchedule).Distinct())
                    {
                        var s = await schedule.GetScheduleByIdAsync(id);
                        if (s == null) continue;
                        bool shouldAssign = ids.Contains(id);
                        bool isAssigned = s.OESGroupDtos.Any(g => g.Id == groupId);
                        if (shouldAssign == isAssigned) continue;
                        await schedule.UpdateScheduleMetadataAsync(new UpdateScheduleMetadataRequestDto
                        {
                            Id = id,
                            Name = s.Name,
                            Code = s.Code,
                            Description = s.Description,
                            LanguageIds = s.LanguageIds ?? [],
                            ExamVenueIds = s.ExamVenueIds ?? [],
                            ScheduleLocation = s.ScheduleLocation ?? ScheduleLocation.Central,
                            StartDate = s.StartDate,
                            StartTime = s.StartTime,
                            EndDate = s.EndDate,
                            EndTime = s.EndTime,
                            OESGroupDtos = shouldAssign
                                ? AddGroupIfMissing(s.OESGroupDtos, groupId)
                                : s.OESGroupDtos.Where(g => g.Id != groupId).ToList()
                        });
                    }
                    break;

                case ResourceType.Questions:
                case ResourceType.Papers:
                    var prev = await GetAssignedItemIdsAsync(r, groupId);
                    await Task.WhenAll(ids.Union(prev).Distinct().Select(id =>
                        groupService.AssignGroupToResourceAsync(id, groupId, r, ids.Contains(id))));
                    break;

                case ResourceType.ItemBank:
                    var prevIb = await GetAssignedItemIdsAsync(ResourceType.ItemBank, groupId);
                    await Task.WhenAll(ids.Union(prevIb).Distinct().Select(async nodeId =>
                    {
                        var dto = await itemBank.GetItemBankByID(nodeId);
                        if (dto == null) return;
                        bool shouldAssign = ids.Contains(nodeId);
                        bool isAssigned = dto.OESGroupDtos.Any(g => g.Id == groupId);
                        if (shouldAssign == isAssigned) return;
                        if (shouldAssign) dto.OESGroupDtos.Add(new GetOESGroupDto { Id = groupId });
                        else dto.OESGroupDtos = dto.OESGroupDtos.Where(g => g.Id != groupId).ToList();
                        await itemBank.UpdateItemBank(dto);
                    }));
                    break;

                case ResourceType.Ilo:
                    var prevIlo = await GetAssignedItemIdsAsync(ResourceType.Ilo, groupId);
                    await Task.WhenAll(ids.Union(prevIlo).Distinct().Select(async iloId =>
                    {
                        bool shouldAssign = ids.Contains(iloId);

                        // Get current group for this node
                        var iloGroups = await ilo.GetIloGroupsAsync(iloId);
                        var currentGroupIds = iloGroups?.GroupsIds ?? [];
                        if (shouldAssign == currentGroupIds.Contains(groupId)) return;

                        var updatedGroupIds = shouldAssign
                            ? [.. currentGroupIds, groupId]
                            : currentGroupIds.Where(g => g != groupId).ToList();

                        // Get the node to know if it's a root or child
                        var nodeResp = await ilo.GetILORoot(iloId);
                        if (nodeResp?.StatusCode != System.Net.HttpStatusCode.OK) return;

                        ILODto? node = null;
                        if (nodeResp.Data is System.Text.Json.JsonElement je)
                            node = je.Deserialize<ILODto>(new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        else
                            node = nodeResp.Data as ILODto;

                        if (node == null) return;

                        if (node.ParentId == null)
                        {
                            await ilo.EditILORoot(new IloEditDTO
                            {
                                Id = node.Id,
                                Name = node.Name,
                                Code = node.Code ?? string.Empty,
                                Description = node.Description ?? string.Empty,
                                GroupsId = updatedGroupIds
                            });
                        }
                        else
                        {
                            await ilo.EditNodeAsync(new ILOInsertionOrUpdateRequestDto(
                                Id: node.Id,
                                Name: node.Name,
                                Description: node.Description ?? string.Empty,
                                ParentId: node.ParentId,
                                Code: node.Code ?? string.Empty,
                                IsActive: true,
                                OrganizationId: 0,
                                GroupsIds: updatedGroupIds));
                        }
                    }));
                    break;
            }
        }


        #region Private fetch helpers

        private async Task<TableData<ResourceInstanceItem>> FetchQuestionsAsync(PaginationSearchModel p)
        {
            p.FilterObj = new QuestionFilterPaginationModel { _selectedStatus = QuestionStatus.Approved };
            var data = await question.GetAllQuestionAsync(p);
            return new TableData<ResourceInstanceItem>
            {
                TotalItems = data?.TotalItems ?? 0,
                Items = data?.Items?.Select(q => new ResourceInstanceItem
                {
                    Id = q.Id,
                    Name = q.Code ?? $"Q-{q.Id}",
                }).ToList() ?? []
            };
        }

        private async Task<TableData<ResourceInstanceItem>> FetchPapersAsync(PaginationSearchModel p)
        {
            var data = await paper.GetAllUserPapersAsync(p);
            return new TableData<ResourceInstanceItem>
            {
                TotalItems = data?.TotalItems ?? 0,
                Items = data?.Items?.Select(x => new ResourceInstanceItem
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToList() ?? []
            };
        }

        private async Task<TableData<ResourceInstanceItem>> FetchScheduleAsync(PaginationSearchModel p)
        {
            var data = await schedule.GetAllSchedulesAsync(p);
            return new TableData<ResourceInstanceItem>
            {
                TotalItems = data?.TotalItems ?? 0,
                Items = data?.Items?.Select(x => new ResourceInstanceItem
                {
                    Id = x.Id,
                    Name = x.Name,
                }).ToList() ?? []
            };
        }

        private static List<GetOESGroupDto> AddGroupIfMissing(List<GetOESGroupDto>? existing, Guid groupId)
        {
            var list = existing ?? [];
            return list.Any(g => g.Id == groupId) ? list : [.. list, new GetOESGroupDto { Id = groupId }];
        }

        #endregion
    }
}