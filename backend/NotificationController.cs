using Microsoft.AspNetCore.Mvc;
using Sample.Core.Constants;
using Sample.Core.Filter;
using Sample.Core.Interfaces;
using Sample.Core.Models.Filter;
using Sample.Core.Models.Inputs;
using Sample.Core.Models.Responses;

namespace Sample.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        public NotificationController(INotificationService NotificationService)
        {
            _notificationService = NotificationService;

        }

        // GET: api/<NotificationController>
        [HasPermission(Permissions = $"{PermissionConstants.NOTIFICATION_VIEW_PERMISSION},{PermissionConstants.NOTIFICATION_EDIT_PERMISSION}")]
        [HttpGet("get-summary-info")]
        public async Task<BaseResponse> GetNotificationByUser([FromQuery] int numberOfNoti, CancellationToken token)
        {
            return await _notificationService.GetUserNotificationsSummaryInfoAsync(numberOfNoti, token);
        }

        // GET: api/<NotificationController>
        [HasPermission(Permissions = $"{PermissionConstants.NOTIFICATION_VIEW_PERMISSION},{PermissionConstants.NOTIFICATION_EDIT_PERMISSION}")]
        [HttpPost("get-list")]
        public async Task<BaseResponse> GetNotificationByUser([FromBody] NotificationFilter input, CancellationToken token)
        {
            return await _notificationService.GetListAsync(input, token);
        }

        // GET: api/<NotificationController>
        [HasPermission(Permissions = $"{PermissionConstants.NOTIFICATION_VIEW_PERMISSION},{PermissionConstants.NOTIFICATION_EDIT_PERMISSION}")]
        [HttpGet]
        public async Task<BaseResponse> GetUserNotification([FromQuery] NotificationFilter input, CancellationToken token)
        {
            return await _notificationService.GetListAsync(input, token);
        }

        // DELETE api/<NotificationController>/5
        [HasPermission(Permissions = $"{PermissionConstants.NOTIFICATION_EDIT_PERMISSION}")]
        [HttpDelete("{id}")]
        public async Task<BaseResponse> DeleteNotification(Guid id, CancellationToken token)
        {
            return await _notificationService.DeleteAsync(id, token);
        }

        [HttpPost("set-notification-state")]
        [HasPermission(Permissions = $"{PermissionConstants.NOTIFICATION_EDIT_PERMISSION}")]
        public async Task<BaseResponse> SetNotificationsAsReadAsync([FromBody] UpdateNotificationStateInput input, CancellationToken token)
        {
            return await _notificationService.SetNotificationsStateAsync(input, token);
        }
    }
}
