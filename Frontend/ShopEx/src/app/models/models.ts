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
  phoneNumber?: string;
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
  quantity: number;
  unitPrice: number;
  taxRate?: number;
  totalPrice: number;
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
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  description?: string;
}

export interface SupplierInvoiceStatement {
  id: number;
  invoiceNumber: string;
  invoiceDate: string;
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
  totalRemainingAmount: number;
  invoices: SupplierInvoiceStatement[];
  returns?: SupplierReturnStatement[];
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
  cost: number;
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

export interface MaintenanceTicketSummary {
  id: number;
  ticketNumber: string;
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
  totalCost: number;
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
  cost: number;
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
  quantityOnHand: number;
  averageCost: number;
  reorderLevel?: number;
  lastUpdatedAt: string;
  totalValue: number;
  isLowStock: boolean;
}

export interface StockMovement {
  id: number;
  warehouseID: number;
  warehouseName: string;
  rmsItemID: number;
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
  name: string;
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
  closedAt?: string;
  notes?: string;
}

export interface BalanceClosureResponse {
  id: number;
  balanceId: number;
  amountUsed: number;
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
  closedAt?: string;
  notes?: string;
}

export interface BalanceTransactionItem {
  id: number;
  transactionType: string;
  storeId: number;
  storeName?: string;
  balanceType: BalanceType;
  balanceTypeName: string;
  amount: number;
  transactionDate: string;
  notes?: string;
  balanceId?: number;
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

export interface UpdateExpenseRequest {
  categoryId: number;
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
}

export interface ExpenseByCategorySummaryDto {
  categoryId: number;
  categoryName: string;
  totalAmount: number;
  count: number;
  percentage: number;
}

export interface ExpenseSummaryDto {
  totalAmount: number;
  totalCount: number;
  items: ExpenseResponse[];
  categorySummaries: ExpenseByCategorySummaryDto[];
}
