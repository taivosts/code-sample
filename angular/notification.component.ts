import { Component, OnInit } from '@angular/core';
import { SortEvent } from 'primeng/api';
import { PageConstant } from '../../../app/core/constants/page.constant';
import { ApiPagingRequest } from '../../../app/core/models/api-paging-request.model';
import {
  NotificationListViewModel,
  NotificationState,
} from '../../../app/core/models/notification.model';
import { NotificationService } from '../../../app/core/services/notification.service';
import {
  FilterEvent,
  ListViewModel,
  SelectionMode,
  isFilterChanges,
} from '../../../app/shared/components/list-view/list-view.model';
import { PermissionNames } from 'src/app/core/constants/permissions.constant';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-notification',
  templateUrl: './notification.component.html',
  styleUrls: ['./notification.component.scss'],
})
export class NotificationComponent implements OnInit {
  allPermissions = PermissionNames;

  currentFilters: FilterEvent[] = [];

  constructor(
    private notificationService: NotificationService,
    private readonly translateService: TranslateService
    ) {}

  public gridData: ListViewModel = {
    columnHeaders: [
      {
        field: 'typeText',
        header: 'Type',
        fieldType: '',
        canFilter: false,
        canSort: true,
      },
      {
        field: 'stateText',
        header: 'State',
        fieldType: '',
        canFilter: false,
        canSort: true,
      },
      {
        field: 'title',
        fieldFilter: 'title',
        header: 'Title',
        fieldType: '',
        canFilter: true,
        canSort: true,
        iconClass: 'fas fa-laugh-squint',
        inputType: 'text'
      },
      {
        field: 'content',
        fieldFilter: 'content',
        header: 'Content',
        fieldType: '',
        canFilter: true,
        canSort: true,
        iconClass: 'fas fa-smile',
        inputType: 'text'
      },
      {
        field: 'url',
        header: 'Navigate Url',
        fieldType: '',
        canFilter: false,
        canSort: false,
      },
      {
        field: 'createdDate',
        header: 'Created Date',
        fieldType: 'date',
        canFilter: false,
        canSort: true,
      },
      {
        field: '',
        header: '',
        fieldType: 'notificationAction',
      },
    ],
    data: [],
    totalRecords: 0,
    limitItem: PageConstant.pageSize,
  };
  isUpdateStateAvailable = false;
  request = new ApiPagingRequest();
  selectedItems: NotificationListViewModel[] = [];
  selectionMode = SelectionMode.multiple;

  clearSelectionFlag = false;

  ngOnInit(): void {
    this.request.pageSize = this.gridData.limitItem;
    this.getNofitications();
  }

  getNofitications() {
    this.notificationService
      .getCurrentUserNotifications(this.request)
      .subscribe((response) => {
        this.selectedItems = [];
        this.gridData.totalRecords = response.data.count;
        this.clearSelectionFlag = false;
        this.isUpdateStateAvailable = false;

        const items = response.data.data.map((item) => {
          item.content = this.translateService.instant(item.content);
          if (item.state === NotificationState.Read)
            return new NotificationListViewModel(item, 'table_row--read');
          else return new NotificationListViewModel(item);
        });

        this.gridData.data = items;
      });
  }

  onPageChange(pageNumber: any) {
    this.request.pageNumber = pageNumber;
    this.getNofitications();
  }

  onSearchChange(searchText: string) {
    this.request.search = searchText;
    this.getNofitications();
  }

  onClear() {
    this.request = new ApiPagingRequest();
    this.getNofitications();
  }
  onSelectItem(items: []) {
    this.selectedItems = [];
    this.selectedItems = items;
    this.isUpdateStateAvailable = items.length > 0;
  }
  onMarkStatusClick(event: any) {
    let newState = event.state;
    if (event.state == NotificationState.Read) {
      newState = NotificationState.Unread;
    } else {
      newState = NotificationState.Read;
    }
    this.notificationService
      .setNotificationState([event.id], false, newState)
      .subscribe(() => {
        this.getNofitications();
      });
  }
  onSetMultipleNotificationsState(isRead: boolean) {
    if (this.selectedItems.length > 0) {
      const ids = this.selectedItems.map((r) => {
        return r.id;
      });
      const state = isRead ? NotificationState.Read : NotificationState.Unread;
      this.notificationService
        .setNotificationState(ids, false, state)
        .subscribe(() => {
          this.clearSelectionFlag = true;
          this.getNofitications();
        });
    }
  }
  onDeleteClick(event: any) {
    this.notificationService.deletenotification(event.id).subscribe(() => {
      this.getNofitications();
    });
  }

  onSortChange(event: SortEvent) {
    if (event.field) this.request.sortBy = event.field;

    if (event.order)
      this.request.sortDirection = event.order == 1 ? 'ASC' : 'DESC';

    this.request.pageNumber = 1;
    this.getNofitications();
  }

  onFilterChange(incomingFilters: FilterEvent[]) {
    if (!isFilterChanges(this.currentFilters, incomingFilters)) {
      this.currentFilters = incomingFilters.map((x) => Object.assign({}, x));
      return;
    }

    if (incomingFilters.length === 0) this.request = new ApiPagingRequest();
    else this.request = new ApiPagingRequest(null, incomingFilters);

    this.currentFilters = incomingFilters.map((x) => Object.assign({}, x));
    this.getNofitications();
  }
}
