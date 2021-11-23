export const Links = {
  Unauthorized: {
    Login: '/login',
  },
  Authorized: {
    Dashboard: '/',
    Users: '/users',
    Notifications: '/notifications',
    Products: '/products',
    ProductDetails: (id?: number) => `/products/${id ?? ':id'}`,
    CreateProduct: '/products/create',
    UiKit: '/uikit',
  },
};
