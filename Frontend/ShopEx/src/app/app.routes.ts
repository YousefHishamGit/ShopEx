import { Routes } from '@angular/router';

// Every page is lazy-loaded so the initial bundle only contains the app shell.
const suppliersList = () => import('./pages/suppliers-list/suppliers-list.component').then(m => m.SuppliersListComponent);
const supplierStatement = () => import('./pages/supplier-statement/supplier-statement.component').then(m => m.SupplierStatementComponent);
const supplierItems = () => import('./pages/supplier-items/supplier-items.component').then(m => m.SupplierItemsComponent);
const openWorksheet = () => import('./pages/open-worksheet/open-worksheet.component').then(m => m.OpenWorksheetComponent);
const invoicesList = () => import('./pages/invoices-list/invoices-list.component').then(m => m.InvoicesListComponent);
const createInvoice = () => import('./pages/create-invoice/create-invoice.component').then(m => m.CreateInvoiceComponent);
const invoiceDetail = () => import('./pages/invoice-detail/invoice-detail.component').then(m => m.InvoiceDetailComponent);
const maintenanceList = () => import('./pages/maintenance/maintenance-list/maintenance-list.component').then(m => m.MaintenanceListComponent);
const maintenanceCreate = () => import('./pages/maintenance/maintenance-create/maintenance-create.component').then(m => m.MaintenanceCreateComponent);
const maintenanceDetail = () => import('./pages/maintenance/maintenance-detail/maintenance-detail.component').then(m => m.MaintenanceDetailComponent);
const stock = () => import('./pages/stock/stock.component').then(m => m.StockComponent);
const consignmentsList = () => import('./pages/consignments/consignments-list/consignments-list.component').then(m => m.ConsignmentsListComponent);
const createConsignment = () => import('./pages/consignments/create-consignment/create-consignment.component').then(m => m.CreateConsignmentComponent);
const consignmentDetail = () => import('./pages/consignments/consignment-detail/consignment-detail.component').then(m => m.ConsignmentDetailComponent);
const customerStatementsList = () => import('./pages/customer-statements/customer-statements-list/customer-statements-list.component').then(m => m.CustomerStatementsListComponent);
const customerStatementDetail = () => import('./pages/customer-statements/customer-statement-detail/customer-statement-detail.component').then(m => m.CustomerStatementDetailComponent);
const balances = () => import('./pages/balances/balances.component').then(m => m.BalancesComponent);
const expenses = () => import('./pages/expenses/expenses.component').then(m => m.ExpensesComponent);

export const routes: Routes = [
  { path: '', redirectTo: 'suppliers', pathMatch: 'full' },
  { path: 'suppliers', loadComponent: suppliersList },
  { path: 'suppliers/:id', loadComponent: supplierStatement },
  { path: 'suppliers/:id/statement', loadComponent: supplierStatement },
  { path: 'suppliers/:id/items', loadComponent: supplierItems },
  { path: 'worksheets', loadComponent: openWorksheet },
  { path: 'worksheets/new', loadComponent: openWorksheet },
  { path: 'invoices', loadComponent: invoicesList },
  { path: 'invoices/new', loadComponent: createInvoice },
  { path: 'invoices/:id', loadComponent: invoiceDetail },
  { path: 'maintenance', loadComponent: maintenanceList },
  { path: 'maintenance/new', loadComponent: maintenanceCreate },
  { path: 'maintenance/:id', loadComponent: maintenanceDetail },
  { path: 'stock', loadComponent: stock },
  { path: 'supplier-returns', redirectTo: 'suppliers', pathMatch: 'full' },
  { path: 'consignments', loadComponent: consignmentsList },
  { path: 'consignments/new', loadComponent: createConsignment },
  { path: 'consignments/:id', loadComponent: consignmentDetail },
  { path: 'customer-statements', loadComponent: customerStatementsList },
  { path: 'customer-statements/:id', loadComponent: customerStatementDetail },
  { path: 'balances', loadComponent: balances },
  { path: 'expenses', loadComponent: expenses },
  { path: '**', redirectTo: 'suppliers' }
];
