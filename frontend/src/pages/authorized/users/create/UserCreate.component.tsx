import React, { useCallback } from 'react';
import {
  CreateUserDto,
  ICreateUserDto,
} from '../../../../services/api/api-client';
import { Input } from '../../../../components/uikit/inputs/Input';
import { Field } from 'components/uikit/Field';
import { useScopedTranslation } from '../../../../application/localization/useScopedTranslation';
import { useAdvancedForm } from '../../../../helpers/form/useAdvancedForm';
import { QueryFactory } from '../../../../services/api';
import { useQueryClient } from 'react-query';
import { requiredRule } from '../../../../helpers/form/react-hook-form-helper';
import { UserCreateFormProps } from './UserCreate';
import { FormError } from '../../../../components/uikit/FormError';
import { Button } from '../../../../components/uikit/buttons/Button';

export const UserCreateComponent = (props: UserCreateFormProps) => {
  const i18n = useScopedTranslation('Page.UserForm');
  const queryClient = useQueryClient();

  const form = useAdvancedForm<ICreateUserDto>(
    useCallback(
      async (data) => {
        await QueryFactory.UserQuery.Client.createUser(new CreateUserDto(data));
        await queryClient.invalidateQueries(
          QueryFactory.UserQuery.getAllUsersQueryKey(),
        );

        props.onClose();
      },
      [props.onClose],
    ),
  );

  return (
    <form onSubmit={form.handleSubmitDefault}>
      <Field title={i18n.t('FirstName')}>
        <Input
          {...form.register('firstName', { ...requiredRule() })}
          errorText={form.formState.errors.firstName?.message}
        />
      </Field>

      <Field title={i18n.t('LastName')}>
        <Input
          {...form.register('lastName', { ...requiredRule() })}
          errorText={form.formState.errors.lastName?.message}
        />
      </Field>

      <Field title={i18n.t('Login')}>
        <Input
          {...form.register('login', { ...requiredRule() })}
          errorText={form.formState.errors.login?.message}
        />
      </Field>

      <FormError>{form.overallError}</FormError>
      <Button type={'submit'} title={i18n.t('CreateButton')} />
    </form>
  );
};
