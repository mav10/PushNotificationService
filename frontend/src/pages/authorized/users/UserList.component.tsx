import React, { useMemo } from 'react';
import { StringParam, useQueryParams } from 'use-query-params';
import {
  pagingSortingQueryParams,
  pagingSortingToBackendRequest,
} from '../../../helpers/pagination-helper';
import { QueryFactory } from '../../../services/api';
import { AppTable, emptyArray } from '../../../components/uikit/table/AppTable';
import { TablePagination } from '../../../components/uikit/TablePagination';
import { Loading } from '../../../components/uikit/suspense/Loading';
import { useSortBy, useTable } from 'react-table';
import { UserDto } from '../../../services/api/api-client';
import { useUpdateSortByInUrl } from '../../../components/uikit/table/useUpdateSortByInUrl';
import { Button } from 'components/uikit/buttons/Button';
import { AppModalContainer } from '../../../components/uikit/modal/Modal.component';
import { useModal } from 'application/hooks/useModal';
import { UserCreateComponent } from './create/UserCreate.component';

const styles = require('./UserList.module.scss');

export const UserList = () => {
  const { visible, closeModal, openModal } = useModal('CLOSED');
  const [queryParams, setQueryParams] = useQueryParams({
    search: StringParam,
    ...pagingSortingQueryParams(5),
  });
  const usersQuery = QueryFactory.UserQuery.useGetAllUsersQuery({
    search: queryParams.search,
    ...pagingSortingToBackendRequest(queryParams),
  });

  const table = useTable<UserDto>(
    {
      data: usersQuery.data?.data ?? emptyArray,
      columns: useMemo(() => {
        return [
          {
            accessor: 'uniqueId',
            Cell: ({ row }) => <div>{row.original.uniqueId}</div>,
            width: 'auto',
            Header: 'Id',
          },
          {
            accessor: 'fullName',
            Cell: ({ row }) => <div>{row.original.fullName}</div>,
            width: 'auto',
            Header: 'Name',
          },
        ];
      }, []),
      manualSortBy: true,
      initialState: useMemo(
        () => ({
          sortBy: queryParams.sortBy
            ? [
                {
                  id: queryParams.sortBy,
                  desc: queryParams.desc,
                },
              ]
            : [],
        }),
        [],
      ),
    },
    useSortBy,
  );
  useUpdateSortByInUrl(table.state.sortBy);

  return (
    <div className={styles.container}>
      <div className={styles.topHeader}>
        <span>Users</span>
        <Button onClick={openModal} title={'Create new user'} />
      </div>
      <Loading loading={usersQuery.isLoading}>
        <div className={styles.tableContainer}>
          <AppTable table={table} />
        </div>
        <TablePagination
          page={queryParams.page}
          perPage={queryParams.perPage}
          totalCount={usersQuery.data?.totalCount ?? 0}
          changePagination={setQueryParams}
        />
      </Loading>

      <AppModalContainer
        title={'User creation'}
        visible={visible}
        onHide={closeModal}
      >
        <UserCreateComponent onClose={closeModal} />
      </AppModalContainer>
    </div>
  );
};
