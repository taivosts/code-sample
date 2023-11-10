using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Sample.Core.Constants;
using Sample.Core.Helpers;
using Sample.Core.Interfaces;
using Sample.Core.Models;
using Sample.Core.Models.Filter;
using Sample.Core.Models.Inputs;
using Sample.Core.Models.Responses;
using Sample.Data.Entities;
using Sample.Data.enums;
using Sample.Data.Repositories.Interfaces;
using Sample.Data.UnitOfWork;
using System.Linq.Dynamic.Core;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Sample.Core.Services
{
    public class NotificationService : BaseService, INotificationService
    {
        private readonly ISearchService<Notification> _searchService;
        private readonly IJwtService _jwtService;
        private readonly IHubContext<MessageHub, IMessageHubClient> _messageHub;
        public NotificationService(
            ISearchService<Notification> searchService,
            IJwtService jwtService,
           IUnitOfWork unitOfWork, 
           IHubContext<MessageHub, IMessageHubClient> messageHub) : base(unitOfWork)
        {
            _searchService = searchService;
            _jwtService = jwtService;
            _messageHub = messageHub;
        }

        public async Task SendSuccessNoticationAsync<T>(string content, List<Guid> userIds, T action = default) where T : BaseAction
        {
            await this.SendNotificationAsync(content, userIds, NotificationType.Success, action);
        }

        public async Task SendFailNoticationAsync<T>(string content, List<Guid> userIds, T action = default) where T : BaseAction
        {
            await this.SendNotificationAsync(content, userIds, NotificationType.Fail, action);
        }

        public async Task SendWarningNotiAsync<T>(string content, List<Guid> userIds, T action = default) where T : BaseAction
        {
            await this.SendNotificationAsync(content, userIds, NotificationType.Warning, action);
        }

        public async Task SendInfoNotiAsync<T>(string content, List<Guid> userIds, T action = default) where T : BaseAction
        {
            await this.SendNotificationAsync(content, userIds, NotificationType.Information, action);
        }

        public async Task<BaseResponse> GetListAsync(NotificationFilter filter, CancellationToken token = default)
        {
            BaseResponse response = new BaseResponse();
            var userId = _jwtService.GetUserId();
            //UserId is only for Notification Entity
            //and only for this function
            //with others, which do not require search by user ID, do not add into SearchFields.
            filter.SearchFields.Add(CommonConstants.UserId, userId.ToString());
            var items = _searchService.GetListWithoutPaging(filter);
            var count = await items.CountAsync(token);
            items = items.OrderByDescending(x => x.CreatedDate);
            if (filter.PageNumber > 0 && filter.PageSize > 0)
            {
                items = items.Paging(filter.PageNumber, filter.PageSize);
            }

            var notifications = items.AsEnumerable().Select(r =>
            {
                Type classType = GetActionClassType(r.ActionType);

                var action = JsonConvert.DeserializeObject(r.Action, classType);

                var notification = new NotificationModel
                {
                    Id = r.Id,
                    Content = r.Content,
                    CreatedDate = r.CreatedDate,
                    State = r.State,
                    StateText = r.State.ToString(),
                    Type = r.Type,
                    TypeText = r.Type.ToString(),
                    Action = action,
                    ActionType = r.ActionType,
                    ActionTypeText = r.ActionType.ToString(),
                    From = r.CreatedBy
                };

                return notification;
            });

            var rst = new PagedListItem<NotificationModel>(filter.PageNumber, filter.PageSize, count, notifications.ToList());
            response.SetSuccessData(rst);
            return response;
        }

        public async Task<BaseResponse> GetUserNotificationsSummaryInfoAsync(int numberOfNoti = 5, CancellationToken token = default)
        {
            BaseResponse response = new BaseResponse();
            var userId = _jwtService.GetUserId();

            //add current user id into search fields
            var filter = new NotificationFilter();
            filter.SearchFields.Add(CommonConstants.UserId, userId.ToString());
            var allItems = _searchService.GetListWithoutPaging(filter);

            var total = await allItems.CountAsync(token);
            var totalUnread = await allItems.CountAsync(x => x.State == NotificationState.Unread, token);
            var totalRead = await allItems.CountAsync(x => x.State == NotificationState.Read, token);
            allItems = allItems.OrderByDescending(x => x.CreatedDate).Paging(1, numberOfNoti);

            var items = allItems.AsEnumerable().Select(r =>
            {
                Type classType = GetActionClassType(r.ActionType);
                return new NotificationModel
                {
                    Id = r.Id,
                    Content = r.Content,
                    CreatedDate = r.CreatedDate,
                    State = r.State,
                    StateText = r.State.ToString(),
                    Type = r.Type,
                    TypeText = r.Type.ToString(),
                    Action = JsonConvert.DeserializeObject(r.Action, classType),
                    ActionType = r.ActionType,
                    ActionTypeText = r.ActionType.ToString(),
                    From = r.CreatedBy
                };
            });

            var result = new UserNotificationSummaryInfoModel()
            {
                UserId = userId,
                Notifications = items.ToList(),
                Total = total,
                TotalRead = totalRead,
                TotalUnread = totalUnread
            };

            response.SetSuccessData(result);
            return response;
        }

        private static Type GetActionClassType(NotificationActionType actionType)
        {
            return actionType switch
            {
                NotificationActionType.File => typeof(FileAction),
                _ => typeof(BaseAction)
            };
        }

        private static NotificationActionType GetActionType<T>(T action) where T : BaseAction
        {
            return action switch
            {
                FileAction => NotificationActionType.File,
                _ => NotificationActionType.Base
            };
        }

        private async Task SendNotificationAsync<T>(string content, List<Guid> userIds, NotificationType type, T action = default, CancellationToken token = default) where T : BaseAction
        {
            var notification = new Notification
            {
                Content = content,
                Type = type,
                ActionType = GetActionType(action),
                Action = JsonConvert.SerializeObject(action)
            };

            var notificationList = userIds.Select(x =>
            {
                var notifyClone = CloneHelper.DeepCopy(notification);
                notifyClone.UserId = x;
                notifyClone.Id = Guid.NewGuid();
                return notifyClone;

            }).ToList();

            await _unitOfWork.NotificationRepository.AddRangeAsync(notificationList, token);
            await _unitOfWork.SaveChangesAsync(token);
            await _messageHub.Clients.All.NewNotiAsync(userIds);
        }

        public async Task<BaseResponse> SetNotificationsStateAsync(UpdateNotificationStateInput input, CancellationToken token = default)
        {
            var response = new BaseResponse();
            var result = await _unitOfWork.NotificationRepository.BulkUpdateNotificationsAsync(input.IsSetAll, input.Ids, input.State, token);
            if (result)
            {
                response.SetSuccessData(result);
            }
            else
            {
                response.SetErrorMessage(ErrorCodeConstants.UPDATE_NOTIFICATION_FAIL);

            }

            return response;
        }

        public async Task<BaseResponse> DeleteAsync(Guid id, CancellationToken token = default)
        {
            var notification = await _unitOfWork.NotificationRepository.GetNotifcationByIdAsync(id, token);
            var response = new BaseResponse();
            if (notification == null)
            {
                response.SetErrorMessage(ErrorCodeConstants.NOTIFICATION_NOT_FOUND);
                return response;
            }

            _unitOfWork.NotificationRepository.SoftDelete(notification);
            await _unitOfWork.SaveChangesAsync(token);
            response.SetSuccessData(notification);

            return response;
        }
    }
}
