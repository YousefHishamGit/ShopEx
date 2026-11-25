export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface Supplier {
  id: number;
  supplierName: string;
  code: string;
  accountNumber?: string;
  taxNumber?: string;
  contactName?: string;
  phone?: string;
  emailAddress?: string;
  address1?: string;
  city?: string;
  country?: string;
}

export interface SupplierItem {
  id: number;
  description: string;
  itemLookupCode?: string;
  price?: number;
  cost?: number;
  quantity?: number;
  taxRate?: number;
}

export interface SupplierInvoiceItem {
  id: number;
  rmsItemID: number;
  itemDescription?: string;
  itemLookupCode?: string;
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  totalPrice: number;
}

export interface SupplierInvoicesPage {
  items: SupplierInvoice[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  totalAmount: number;
  totalPaid: number;
  totalRemaining: number;
}

export interface SupplierPayment {
  id: number;
  amount: number;
  paymentDate: string;
  paymentMethod?: string;
  createdAt?: string;
}

export interface SupplierInvoice {
  id: number;
  rmsSupplierID: number;
  rmsSupplierName?: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate?: string;
  subTotal?: number;
  discountType?: number;   // 0 none, 1 percent, 2 amount
  discountValue?: number;
  discountAmount?: number;
  paymentType: number;
  paymentTypeName?: string;
  creditDays?: number;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  status: number;
  statusName?: string;
  notes?: string;
  createdAt?: string;
  items: SupplierInvoiceItem[];
  payments: SupplierPayment[];
}

export interface SupplierPaymentStatement {
  id: number;
  amount: number;
  paymentDate: string;
  paymentMethod?: string;
}

export interface SupplierInvoiceItemStatement {
  id: number;
  rmsItemID: number;
  itemDescription?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  description?: string;
}

export interface SupplierInvoiceStatement {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
  subTotal?: number;
  discountAmount?: number;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  status: string;
  paymentType: string;
  dueDate?: string;
  items: SupplierInvoiceItemStatement[];
  payments: SupplierPaymentStatement[];
}

export interface SupplierReturnStatement {
  id: number;
  returnNumber: string;
  returnDate: string;
  warehouseID: number;
  warehouseName: string;
  totalAmount: number;
  refundedAmount: number;
  pendingRefund: number;
  status: string;
  settlementType: string;
  itemsCount: number;
  notes?: string;
}

export interface SupplierStatement {
  supplierId: number;
  supplierName: string;
  code: string;
  accountNumber?: string;
  phoneNumber?: string;
  emailAddress?: string;
  totalInvoicesAmount: number;
  totalPaidAmount: number;
  totalReturnsAmount?: number;
  totalRefundsAmount?: number;
  totalPendingRefundsAmount?: number;
  totalRemainingAmount: number;

  // Counts (always returned from server)
  unpaidCount: number;
  partialCount: number;
  paidCount: number;
  totalInvoicesCount: number;

  // Invoice pagination metadata
  invoicesPageNumber: number;
  invoicesPageSize: number;
  invoicesTotalPages: number;
  invoicesTotalCount: number;

  // Returns metadata
  returnsCount: number;
  returnsPageNumber: number;
  returnsPageSize: number;
  returnsTotalPages: number;
  returnsTotalCount: number;

  invoices: SupplierInvoiceStatement[];
  returns: SupplierReturnStatement[];
}

export interface CreateInvoiceItemRequest {
  rmsItemID?: number;
  quantity: number;
  unitPrice: number;
  description?: string;
  itemId?: number;
  taxRate?: number;
}

export interface CreateInvoiceRequest {
  rmsSupplierID?: number;
  supplierId?: number;
  warehouseID?: number;
  invoiceNumber: string;
  invoiceDate?: string;
  paymentType: number; // 1 = Cash, 2 = Credit
  creditDays?: number;
  notes?: string;
  discountType?: number;   // 0 none, 1 percent, 2 amount
  discountValue?: number;
  initialPayment?: number;
  initialPaymentMethod?: string;
  dueDate?: string;
  items: CreateInvoiceItemRequest[];
}

export interface AddPaymentRequest {
  amount: number;
  paymentDate?: string;
  paymentMethod?: string;
}

export interface WorksheetPurchaseOrder {
  id: number;
  worksheetID?: number;
  itemID?: number;
  itemDescription?: string;
  quantityOrdered?: number;
  price?: number;
  storeID?: number;
  taxRate?: number;
  orderNumber?: string;
  detailID?: number;
}

export interface Worksheet {
  id: number;
  style?: number;
  effectiveDate?: string;
  status?: number;
  statusName?: string;
  notes?: string;
  title?: string;
  fromDate?: string;
  purchaseOrders: WorksheetPurchaseOrder[];
}

export interface PurchaseLineDto {
  itemId: number;
  description?: string;
  qty: number;
  price: number;
  storeId: number;
  taxRate: number;
}

export interface OpenWorksheetDto {
  title?: string;
  notes?: string;
}

export interface OpenWorksheetWithPurchasesRequest {
  title?: string;
  notes?: string;
  items: PurchaseLineDto[];
}

// ============================================
// Maintenance Module Models (قسم الصيانة)
// ============================================

export enum MaintenanceStatus {
  Received = 1,       // تم الاستلام
  UnderRepair = 2,    // قيد الفحص والإصلاح
  Completed = 3,      // تم الإصلاح وجاهز للتسليم
  Delivered = 4,      // تم التسليم للعميل والدفع
  Cancelled = 5       // ملغي / تعذر الإصلاح
}

export enum MaintenancePaymentMethod {
  Cash = 1,           // نقدي
  CreditCard = 2,     // فيزا / كارت
  ElectronicWallet = 3 // محفظة إلكترونية
}

export interface MaintenanceTicketOperation {
  id: number;
  maintenanceTicketId: number;
  operationName: string;
  /** السعر على العميل (هو اللي يظهر في الفاتورة) */
  cost: number;
  /** التكلفة علينا — للإدارة فقط (undefined للموظف) */
  partCost?: number;
  notes?: string;
  createdAt: string;
}

export interface MaintenancePayment {
  id: number;
  maintenanceTicketId: number;
  amount: number;
  paidAt: string;
  paymentMethod: MaintenancePaymentMethod;
  paymentMethodName: string;
  notes?: string;
}

export interface PagedMaintenanceTickets {
  items: MaintenanceTicketSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  countTotal: number;
  countReceived: number;
  countCompleted: number;
  countDelivered: number;
  /** تكلفة القطع علينا للنتائج الحالية — للإدارة فقط */
  totalPartCost?: number | null;
  /** الربح (السعر − التكلفة) للنتائج الحالية — للإدارة فقط */
  totalProfit?: number | null;
}

export interface MaintenanceTicketSummary {
  id: number;
  ticketNumber: string;
  storeId?: number | null;
  storeName?: string | null;
  customerName: string;
  customerPhone: string;
  deviceName: string;
  receivedAt: string;
  status: MaintenanceStatus;
  statusName: string;
  totalCost: number;
  paidAmount: number;
  remainingAmount: number;
  isPaid: boolean;
}

export interface MaintenanceTicket {
  id: number;
  ticketNumber: string;
  receivedBy: string;
  storeId?: number | null;
  storeName?: string | null;
  customerName: string;
  customerPhone: string;
  deviceName: string;
  brand?: string;
  model?: string;
  color?: string;
  serialNumber?: string;
  issueDescription?: string;
  accessories?: string;
  notes?: string;
  receivedAt: string;
  expectedCost: number;
  status: MaintenanceStatus;
  statusName: string;
  repairedAt?: string;
  repairNotes?: string;
  deliveredAt?: string;
  deliveryNotes?: string;
  /** ملاحظة الفاتورة / الضمان (تُطبع على الفاتورة) */
  warrantyNote?: string;
  totalCost: number;
  /** إجمالي التكلفة علينا — للإدارة فقط */
  totalPartCost?: number;
  /** الربح = السعر - التكلفة — للإدارة فقط */
  profit?: number;
  paidAmount: number;
  remainingAmount: number;
  isPaid: boolean;
  operations: MaintenanceTicketOperation[];
  payments: MaintenancePayment[];
}

export interface CreateMaintenanceTicketRequest {
  receivedBy?: string;
  customerName: string;
  customerPhone: string;
  deviceName: string;
  brand?: string;
  model?: string;
  color?: string;
  serialNumber?: string;
  issueDescription?: string;
  accessories?: string;
  notes?: string;
  expectedCost?: number;
  deposit?: number;
  depositPaymentMethod?: MaintenancePaymentMethod;
}

export interface AddTicketOperationRequest {
  operationName: string;
  /** السعر على العميل */
  cost: number;
  /** التكلفة علينا */
  partCost?: number;
  notes?: string;
}

export interface CompleteRepairRequest {
  repairNotes?: string;
  operations?: AddTicketOperationRequest[];
}

export interface DeliverTicketRequest {
  paymentAmount?: number;
  paymentMethod?: MaintenancePaymentMethod;
  deliveryNotes?: string;
  warrantyNote?: string;
}

export interface AddTicketPaymentRequest {
  amount: number;
  paymentMethod: MaintenancePaymentMethod;
  notes?: string;
  paidAt?: string;
}

// ----------------------------------------------------
// Stock & Warehouse Models
// ----------------------------------------------------
export interface Warehouse {
  id: number;
  name: string;
  location?: string;
  isDefault: boolean;
  isActive: boolean;
  totalItemsCount?: number;
}

export interface StockBalance {
  id: number;
  warehouseID: number;
  warehouseName: string;
  rmsItemID: number;
  itemDescription?: string;
  itemLookupCode?: string;
  retailPrice?: number;
  cost?: number;
  quantityOnHand: number;
  averageCost: number;
  reorderLevel?: number;
  lastUpdatedAt: string;
  totalValue: number;
  isLowStock: boolean;
}

export interface PagedStockBalances {
  items: StockBalance[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalQuantity: number;
  totalCost: number;
  totalPrice: number;
  totalPages: number;
}

export interface StockMovement {
  id: number;
  warehouseID: number;
  warehouseName: string;
  rmsItemID: number;
  itemDescription?: string;
  isIncoming: boolean;
  movementType: number;
  movementTypeName: string;
  quantity: number;
  unitCost: number;
  totalCost: number;
  movementDate: string;
  referenceType?: string;
  referenceID?: number;
  notes?: string;
  createdBy?: string;
}

export interface StockAdjustmentRequest {
  warehouseID: number;
  rmsItemID: number;
  quantity: number;
  unitCost: number;
  reason?: string;
  createdBy?: string;
}

// ----------------------------------------------------
// Supplier Return Models
// ----------------------------------------------------
export enum SupplierReturnStatus {
  Draft = 1,
  Confirmed = 2,
  Cancelled = 3
}

export enum SupplierReturnSettlementType {
  CreditToAccount = 1,
  CashRefund = 2,
  Replacement = 3
}

export interface SupplierReturnItem {
  id: number;
  supplierReturnID: number;
  supplierInvoiceItemID?: number;
  rmsItemID: number;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  reason?: string;
}

export interface SupplierRefund {
  id: number;
  supplierReturnID: number;
  amount: number;
  refundDate: string;
  paymentMethod?: string;
  notes?: string;
}

export interface SupplierReturnSummary {
  id: number;
  returnNumber: string;
  rmsSupplierID: number;
  supplierInvoiceID?: number;
  warehouseID: number;
  warehouseName: string;
  returnDate: string;
  status: SupplierReturnStatus;
  statusName: string;
  settlementType: SupplierReturnSettlementType;
  settlementTypeName: string;
  itemsCount: number;
  totalAmount: number;
  refundedAmount: number;
  pendingRefund: number;
}

export interface SupplierReturn {
  id: number;
  returnNumber: string;
  rmsSupplierID: number;
  supplierInvoiceID?: number;
  invoiceNumber?: string;
  warehouseID: number;
  warehouseName: string;
  returnDate: string;
  status: SupplierReturnStatus;
  statusName: string;
  settlementType: SupplierReturnSettlementType;
  settlementTypeName: string;
  notes?: string;
  totalAmount: number;
  refundedAmount: number;
  pendingRefund: number;
  items: SupplierReturnItem[];
  refunds: SupplierRefund[];
}

export interface CreateSupplierReturnItemRequest {
  supplierInvoiceItemID?: number;
  rmsItemID: number;
  quantity: number;
  unitPrice: number;
  reason?: string;
}

export interface CreateSupplierReturnRequest {
  rmsSupplierID: number;
  supplierInvoiceID?: number;
  warehouseID: number;
  returnDate?: string;
  settlementType: SupplierReturnSettlementType;
  notes?: string;
  confirmImmediately?: boolean;
  items: CreateSupplierReturnItemRequest[];
}

export interface AddSupplierRefundRequest {
  amount: number;
  refundDate?: string;
  paymentMethod?: string;
  notes?: string;
}

// ----------------------------------------------------
// Consignment (بنزيمات / بضاعة أمانة) & Dealers Models
// ----------------------------------------------------
export type ConsignmentStatus = 'Open' | 'Closed' | 'Cancelled';

export interface Dealer {
  id: number;
  name: string;
  phone: string;
  address?: string;
  notes?: string;
  isActive: boolean;
  createdAt: string;
  activeConsignmentsCount?: number;
  totalOutstandingValue?: number;
  totalUnpaidAmount?: number;
}

export interface CreateDealerRequest {
  name: string;
  phone: string;
  address?: string;
  notes?: string;
}

export interface UpdateDealerRequest {
  name: string;
  phone: string;
  address?: string;
  notes?: string;
  isActive: boolean;
}

export interface ConsignmentItem {
  id: number;
  consignmentID: number;
  rmsItemID: number;
  itemDescription?: string;
  itemLookupCode?: string;
  quantityIssued: number;
  unitPrice: number;
  unitCost: number;
  quantitySold: number;
  quantityReturned: number;
  quantityOutstanding: number;
  issuedValue: number;
  soldValue: number;
  outstandingValue: number;
  profit: number;
}

export interface ConsignmentSettlementItem {
  id: number;
  consignmentItemID: number;
  rmsItemID: number;
  itemDescription?: string;
  quantitySold: number;
  quantityReturned: number;
  soldValue: number;
}

export interface ConsignmentSettlement {
  id: number;
  consignmentID: number;
  settlementDate: string;
  notes?: string;
  createdBy?: string;
  amountDue: number;
  items: ConsignmentSettlementItem[];
}

export interface ConsignmentPayment {
  id: number;
  consignmentID: number;
  amount: number;
  paymentDate: string;
  paymentMethod?: string;
  notes?: string;
}

export interface Consignment {
  id: number;
  consignmentNumber: string;
  dealerID: number;
  dealerName: string;
  dealerPhone: string;
  warehouseID?: number;
  warehouseName?: string;
  issueDate: string;
  agreedDays?: number;
  dueDate: string;
  status: ConsignmentStatus;
  notes?: string;
  totalIssuedValue: number;
  totalSoldValue: number;
  paidAmount: number;
  remainingAmount: number;
  outstandingQuantity: number;
  outstandingValue: number;
  isFullyAccountedFor: boolean;
  isOverdue: boolean;
  items: ConsignmentItem[];
  settlements: ConsignmentSettlement[];
  payments: ConsignmentPayment[];
}

export interface ConsignmentSummary {
  id: number;
  consignmentNumber: string;
  dealerID: number;
  dealerName: string;
  dealerPhone: string;
  issueDate: string;
  agreedDays?: number;
  dueDate: string;
  status: ConsignmentStatus;
  itemsCount: number;
  totalIssuedValue: number;
  totalSoldValue: number;
  paidAmount: number;
  remainingAmount: number;
  outstandingQuantity: number;
  outstandingValue: number;
  isFullyAccountedFor: boolean;
  isOverdue: boolean;
}

/** One page of consignments + stats for the selected dealer (stats ignore the status tab and search). */
export interface ConsignmentsPage {
  items: ConsignmentSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  countTotal: number;
  countOpen: number;
  countOverdue: number;
  countClosed: number;
  countCancelled: number;
  totalOutstandingValue: number;
  totalRemainingUnpaid: number;
}

export interface CreateConsignmentItemRequest {
  rmsItemID: number;
  quantityIssued: number;
  unitPrice: number;
  unitCost: number;
}

export interface CreateConsignmentRequest {
  dealerID: number;
  warehouseID?: number;
  agreedDays?: number;
  dueDate?: string;
  notes?: string;
  items: CreateConsignmentItemRequest[];
}

export interface SettlementItemRequest {
  consignmentItemID: number;
  quantitySold: number;
  quantityReturned: number;
}

export interface CreateConsignmentSettlementRequest {
  notes?: string;
  createdBy?: string;
  items: SettlementItemRequest[];
  payment?: AddConsignmentPaymentRequest;
}

export interface AddConsignmentPaymentRequest {
  amount: number;
  paymentDate?: string;
  paymentMethod?: string;
  notes?: string;
}

// ----------------------------------------------------
// RMS Customer Statement & Open Invoices Models
// ----------------------------------------------------
export interface CustomerSummary {
  id: number;
  accountNumber: string;
  customerName: string;
  phoneNumber: string;
  address?: string;
  accountBalance: number;
  openInvoicesCount: number;
  totalOpenInvoicesAmount: number;
  oldestOpenInvoiceDays?: number;
}

export interface CustomerOpenInvoice {
  invoiceID: number;
  invoiceDate?: string;
  daysOld: number;
  customerID: number;
  customerName: string;
  accountNumber: string;
  phoneNumber: string;
  storeID?: number;
  storeName: string;
  totalAmount: number;
  deposit: number;
  remainingAmount: number;
  comment?: string;
  referenceNumber?: string;
  itemsCount: number;
}

export interface CustomerStatement {
  customerID: number;
  accountNumber: string;
  customerName: string;
  phoneNumber: string;
  address?: string;
  accountBalance: number;
  minDaysOldFilter?: number;
  totalOpenInvoicesCount: number;
  totalOpenInvoicesAmount: number;
  totalOpenInvoicesRemaining: number;
  openInvoices: CustomerOpenInvoice[];
}

export interface CustomerInvoiceItem {
  entryID: number;
  itemID?: number;
  itemLookupCode?: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  fullPrice?: number;
  totalPrice: number;
}

export interface CustomerInvoiceDetail {
  invoiceID: number;
  invoiceDate?: string;
  daysOld: number;
  customerID: number;
  accountNumber: string;
  customerName: string;
  phoneNumber: string;
  storeID?: number;
  storeName: string;
  totalAmount: number;
  deposit: number;
  remainingAmount: number;
  comment?: string;
  referenceNumber?: string;
  items: CustomerInvoiceItem[];
}

// ============================================
// Balance & Closures Module (أرصدة الفروع وتقفيلاتها)
// ============================================

export enum BalanceType {
  VodafoneCash = 1,
  OrangeCash = 2,
  EtisalatCash = 3,
  instapay = 4,
  Cards = 5,
  Fawry = 6
}

export interface StoreOption {
  id: number;
  storeId: number;
  name: string;
  storeName: string;
  code?: string;
  city?: string;
  address?: string;
  phone?: string;
}

export interface RMSItem {
  id: number;
  description: string;
  itemLookupCode?: string;
  price?: number;
  priceA?: number;
  cost?: number;
  lastCost?: number;
  quantity?: number;
  supplierID?: number;
}

/** شحن رصيد من الفرع لنفسه (الفرع من السيرفر) */
export interface ChargeOwnBalanceRequest {
  balanceType: BalanceType;
  amount: number;
  notes?: string;
}

export interface ChargeBalanceRequest {
  storeId: number;
  balanceType: BalanceType;
  amount: number;
  chargedAt?: string;
  notes?: string;
}

export interface CloseBalanceRequest {
  amountUsed: number;
  profit: number;
  closedAt?: string;
  notes?: string;
}

export interface BalanceClosureResponse {
  id: number;
  balanceId: number;
  amountUsed: number;
  profit: number;
  closedAt: string;
  notes?: string;
}

export interface BalanceSummaryResponse {
  id: number;
  storeId: number;
  balanceType: BalanceType;
  balanceTypeName: string;
  amount: number;
  chargedAt: string;
  notes?: string;
  totalUsedAmount: number;
  remainingAmount: number;
  closuresCount: number;
  isFullySettled: boolean;
}

export interface BalanceResponse {
  id: number;
  storeId: number;
  balanceType: BalanceType;
  balanceTypeName: string;
  amount: number;
  chargedAt: string;
  notes?: string;
  totalUsedAmount: number;
  remainingAmount: number;
  isFullySettled: boolean;
  closures: BalanceClosureResponse[];
}

export interface StoreBalanceGroup {
  storeId: number;
  storeName?: string;
  balanceType: BalanceType;
  balanceTypeName: string;
  totalCharged: number;
  totalUsed: number;
  totalProfit: number;
  currentBalance: number;
  chargesCount: number;
  closuresCount: number;
  lastChargedAt?: string;
  lastClosedAt?: string;
}

export interface CloseStoreBalanceRequest {
  storeId: number;
  balanceType: BalanceType;
  amountUsed: number;
  profit: number;
  closedAt?: string;
  notes?: string;
}

export interface BalanceTransactionItem {
  id: number;
  transactionType: 'Charge' | 'Closure';
  storeId: number;
  storeName?: string;
  balanceType: BalanceType;
  balanceTypeName: string;
  amount: number;
  profit: number;
  transactionDate: string;
  notes?: string;
  balanceId?: number;
}

/** One page of the balance ledger (newest first). */
export interface BalanceLedgerPage {
  items: BalanceTransactionItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}





export enum ExpensePaymentMethod {
  Cash = 1,
  BankTransfer = 2,
  CreditCard = 3,
  ElectronicWallet = 4,
  Cheque = 5
}

export interface ExpenseCategoryDto {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  expensesCount: number;
  totalAmount: number;
}

export interface CreateExpenseCategoryRequest {
  name: string;
  description?: string;
}

export interface UpdateExpenseCategoryRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface CreateExpenseRequest {
  categoryId?: number;
  categoryName?: string;
  storeId?: number;
  amount: number;
  expenseDate?: string;
  paymentMethod: ExpensePaymentMethod;
  recipient?: string;
  receiptNumber?: string;
  notes?: string;
  createdBy?: string;
}

export interface CreateBranchExpenseRequest {
  categoryId?: number;
  categoryName?: string;
  amount: number;
  paymentMethod: ExpensePaymentMethod;
  recipient?: string;
  notes: string;
}

export interface UpdateExpenseRequest {
  categoryId?: number;
  categoryName?: string;
  storeId?: number;
  amount: number;
  expenseDate: string;
  paymentMethod: ExpensePaymentMethod;
  recipient?: string;
  receiptNumber?: string;
  notes?: string;
}

export interface ExpenseResponse {
  id: number;
  categoryId: number;
  categoryName: string;
  storeId?: number;
  storeName?: string;
  amount: number;
  expenseDate: string;
  paymentMethod: ExpensePaymentMethod;
  paymentMethodName: string;
  recipient?: string;
  receiptNumber?: string;
  notes?: string;
  createdBy?: string;
  createdAt: string;
  /** مصروف تلقائي (شراء جهاز / شحن رصيد): لا يُعدَّل ولا يُحذف من هنا */
  isAutomatic?: boolean;
}

export interface ExpenseByCategorySummaryDto {
  categoryId: number;
  categoryName: string;
  totalAmount: number;
  count: number;
  percentage: number;
}

export interface ExpenseFilters {
  storeId?: number;
  categoryId?: number;
  fromDate?: string;
  toDate?: string;
  paymentMethod?: number;
  search?: string;
}

export interface ExpenseSummaryDto {
  totalAmount: number;
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  items: ExpenseResponse[];
  categorySummaries: ExpenseByCategorySummaryDto[];
}

// ============================================
// Users & Auth Models (المستخدمون وتسجيل الدخول)
// ============================================

export enum UserRole {
  Admin = 1,     // موظف HQ — كل الصلاحيات
  Employee = 2,  // موظف فرع — استلام/تسليم صيانة وتقفيل رصيد فرعه فقط
  Dev = 3        // المطوّر — يتحكم في الاشتراك فقط (جدول منفصل، لا يظهر في المستخدمين)
}

export interface AppUser {
  id: number;
  name: string;
  phone: string;
  role: UserRole;
  roleName: string;
  username: string;
  storeId?: number | null;
  storeName?: string | null;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string | null;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: AppUser;
}

export interface DevLoginResponse {
  token: string;
  expiresAt: string;
  id: number;
  name: string;
  username: string;
}

export type LicenseStatus = 'Active' | 'Disabled' | 'Expired';

export interface LicenseInfo {
  isEnabled: boolean;
  expiresAt: string;
  isValid: boolean;
  status: LicenseStatus;
  daysRemaining: number;
  updatedAt: string;
  updatedBy?: string | null;
}

export interface CreateUserRequest {
  name: string;
  phone: string;
  role: UserRole;
  username: string;
  password: string;
  storeId?: number | null;
}

// ===================== Dashboard =====================

export interface DashboardSales {
  totalSales: number;
  totalCost: number;
  grossProfit: number;
  marginPercent: number;
  transactionsCount: number;
  itemsSold: number;
}

export interface DashboardMaintenance {
  receivedToday: number;
  repairedToday: number;
  deliveredToday: number;
  revenueToday: number;
  underRepairNow: number;
  readyForDeliveryNow: number;
}

export interface DashboardBranch {
  storeId: number | null;
  storeName: string;
  sales: number;
  profit: number;
  transactionsCount: number;
  // الصيانة
  ticketsReceived: number;
  ticketsDelivered: number;
  maintenanceRevenue: number;
  maintenanceProfit: number;
  // الأجهزة (مشتراة بغرض البيع)
  devicesBoughtCount: number;
  devicesBoughtAmount: number;
  devicesSoldCount: number;
  devicesSoldAmount: number;
  devicesProfit: number;
  devicesInStockCount: number;
  devicesInStockValue: number;
  // الأرصدة
  balanceCharged: number;
  balanceClosed: number;
  balanceProfit: number;
  balanceRemaining: number;
  // المصروفات (تشمل شحن الرصيد وشراء الأجهزة)
  expenses: number;
}

export interface DashboardDueInvoice {
  invoiceId: number;
  invoiceNumber: string;
  supplierId: number;
  supplierName: string;
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  /** negative = overdue by that many days, 0 = due today */
  daysToDue: number;
}

export interface DashboardInvoiceDues {
  dueSoonDays: number;
  overdueCount: number;
  overdueAmount: number;
  dueTodayCount: number;
  dueTodayAmount: number;
  dueSoonCount: number;
  dueSoonAmount: number;
  totalOutstanding: number;
  items: DashboardDueInvoice[];
}

export interface DashboardDueConsignment {
  consignmentId: number;
  consignmentNumber: string;
  dealerName: string;
  dealerPhone: string;
  dueDate: string;
  remainingMoney: number;
  outstandingGoodsValue: number;
  daysToDue: number;
}

export interface DashboardConsignmentDues {
  dueSoonDays: number;
  overdueCount: number;
  overdueMoney: number;
  overdueGoodsValue: number;
  dueSoonCount: number;
  openCount: number;
  openMoney: number;
  openGoodsValue: number;
  items: DashboardDueConsignment[];
}

export interface DashboardPnlRow {
  /** branch | hq | maintenance | devices | balance | consignments | total */
  kind: string;
  storeId: number | null;
  label: string;
  revenue: number;
  cost: number;
  profit: number;
  /** مصروفات تشغيلية فقط (شراء الأجهزة وشحن الرصيد محسوبان كتكلفة) */
  expenses: number;
  net: number;
}

export interface DashboardData {
  date: string;
  generatedAt: string;
  sales: DashboardSales;
  maintenance: DashboardMaintenance;
  expenses: { total: number; count: number };
  balances: { chargedToday: number; closedToday: number; profitToday: number };
  supplierInvoices: DashboardInvoiceDues;
  consignments: DashboardConsignmentDues;
  branches: DashboardBranch[];
  pnl: DashboardPnlRow[];
  pnlTotal: DashboardPnlRow;
}

// ================================================================
// Devices (أجهزة مشتراة بغرض البيع: HQ + الفروع)
// ================================================================

export enum DeviceStatus {
  InStock = 1,
  Sold = 2
}

export interface DeviceResponse {
  id: number;
  storeId: number | null;
  storeName: string;
  brand: string;
  model: string;
  color?: string | null;
  storage?: string | null;
  imei?: string | null;
  /** صحة البطارية % (اختياري) */
  batteryHealth?: number | null;
  hasBox: boolean;
  notes?: string | null;
  purchasePrice: number;
  purchaseDate: string;
  sellerName?: string | null;
  status: DeviceStatus;
  salePrice?: number | null;
  soldAt?: string | null;
  buyerName?: string | null;
  buyerPhone?: string | null;
  saleNotes?: string | null;
  soldBy?: string | null;
  soldStoreId?: number | null;
  soldStoreName?: string | null;
  profit?: number | null;
  createdBy?: string | null;
  createdAt: string;
}

export interface DeviceStatsDto {
  totalCount: number;
  inStockCount: number;
  soldCount: number;
  totalPurchase: number;
  inStockValue: number;
  totalSales: number;
  totalProfit: number;
}

export interface DeviceSummaryDto {
  stats: DeviceStatsDto;
  page: number;
  pageSize: number;
  totalPages: number;
  items: DeviceResponse[];
}

export interface DeviceFilters {
  /** 0 = HQ فقط، رقم = فرع معين، undefined = الكل */
  storeId?: number;
  status?: DeviceStatus;
  search?: string;
  fromDate?: string;
  toDate?: string;
}

export interface CreateDeviceRequest {
  /** للأدمن فقط (الموظف يتحدد فرعه من السيرفر). null/undefined = HQ */
  storeId?: number | null;
  brand: string;
  model: string;
  color?: string;
  storage?: string;
  imei?: string;
  batteryHealth?: number | null;
  hasBox: boolean;
  purchasePrice: number;
  purchaseDate?: string;
  sellerName?: string;
  notes?: string;
}

export type UpdateDeviceRequest = Omit<CreateDeviceRequest, 'storeId'>;

export interface SellDeviceRequest {
  salePrice: number;
  /** للأدمن فقط: الفرع البايع (null = HQ). الموظف يتحدد فرعه من السيرفر */
  soldStoreId?: number | null;
  buyerName?: string;
  buyerPhone?: string;
  notes?: string;
}

// ================================================================
// Partners (شركاء الفروع وتوزيع الأرباح)
// ================================================================

export interface PartnerShare {
  id: number;
  name: string;
  percent: number;
  /** حصة الشريك من الصافي بعد الزكاة */
  amount: number;
}

export interface BranchProfitShare {
  storeId: number;
  storeName: string;
  /** الإدارة العامة (storeId = -1) */
  isHq: boolean;
  /** الفروع دائماً مفعَّلة؛ الإدارة يمكن إغلاقها */
  isEnabled: boolean;
  salesProfit: number;
  maintenanceProfit: number;
  devicesProfit: number;
  balanceProfit: number;
  consignmentProfit: number;
  /** مصروفات تشغيلية (بدون شراء الأجهزة وشحن الرصيد) */
  expenses: number;
  totalProfit: number;
  zakatPercent: number;
  zakatAmount: number;
  netAfterZakat: number;
  partners: PartnerShare[];
  totalPercent: number;
  unallocatedAmount: number;
}

export interface PartnersReport {
  from: string;
  to: string;
  branches: BranchProfitShare[];
  pnl: DashboardPnlRow[];
  pnlTotal: DashboardPnlRow;
}

export interface PartnerInput {
  name: string;
  percent: number;
}

export interface SaveBranchPartnersRequest {
  zakatPercent: number;
  partners: PartnerInput[];
}
