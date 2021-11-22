import { Links } from 'application/constants/links';
import { AppTable, emptyArray } from 'components/uikit/table/AppTable';
import { AppLink } from 'components/uikit/buttons/AppLink';
import { ButtonColor } from 'components/uikit/buttons/Button';
import { Input } from 'components/uikit/inputs/Input';
import { Loading } from 'components/uikit/suspense/Loading';
import { TablePagination } from 'components/uikit/TablePagination';
import {
  pagingSortingQueryParams,
  pagingSortingToBackendRequest,
} from 'helpers/pagination-helper';
import { useUpdateSortByInUrl } from 'components/uikit/table/useUpdateSortByInUrl';
import React, { useMemo } from 'react';
import { useSortBy, useTable } from 'react-table';
import { QueryFactory } from 'services/api';
import { ProductListItemDto } from 'services/api/api-client';
import { StringParam, useQueryParams } from 'use-query-params';
const styles = require('./ProductListPage.module.scss');

export const ProductListPage: React.FC = () => {
  const [queryParams, setQueryParams] = useQueryParams({
    search: StringParam,
    ...pagingSortingQueryParams(2),
  });
  const productsQuery = QueryFactory.ProductQuery.useSearchQuery({
    search: queryParams.search,
    ...pagingSortingToBackendRequest(queryParams),
  });

  const table = useTable<ProductListItemDto>(
    {
      data: productsQuery.data?.data ?? emptyArray,
      columns: useMemo(() => {
        return [
          {
            accessor: 'title',
            Cell: ({ row }) => (
              <div>
                {row.original.id}. {row.original.title} (
                {row.original.productType})
              </div>
            ),
            width: 'auto',
            Header: 'Title',
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
    <div>
      <div className={styles.navigation}>
        <AppLink
          color={ButtonColor.Primary}
          to={Links.Authorized.CreateProduct}
        >
          Create product
        </AppLink>
        <AppLink color={ButtonColor.Primary} to={Links.Authorized.UiKit}>
          UiKit
        </AppLink>
      </div>
      <Input
        placeholder={'search'}
        defaultValue={queryParams.search ?? undefined}
        onChange={(e) => setQueryParams({ search: e.target.value, page: 1 })}
      />

      <Loading loading={productsQuery.isLoading}>
        <AppTable table={table} />
        <TablePagination
          page={queryParams.page}
          perPage={queryParams.perPage}
          totalCount={productsQuery.data?.totalCount ?? 0}
          changePagination={setQueryParams}
        />
      </Loading>
    </div>
  );
};
