import {
  Component, Input, Output, EventEmitter, OnInit, OnChanges, OnDestroy, SimpleChanges,
  ViewChild, ElementRef, HostListener, ChangeDetectionStrategy, ChangeDetectorRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

export interface TabbedSelectItem {
  id: number | string;
  title: string;
  subtitle?: string;
  price?: number;
  cost?: number;
  quantity?: number;
  category?: string;
  badge?: string;
  icon?: string;
  raw?: any;
}

interface IndexedItem {
  item: TabbedSelectItem;
  /** pre-lowercased haystack so searching never re-lowercases thousands of strings */
  hay: string;
  cat: string;
}

@Component({
  selector: 'app-tabbed-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tabbed-select.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [`
    @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }
    @keyframes scaleUp { from { transform: scale(0.97); opacity: 0; } to { transform: scale(1); opacity: 1; } }
    .animate-fade-in { animation: fadeIn 0.12s ease-out forwards; }
    .animate-scale-up { animation: scaleUp 0.15s cubic-bezier(0.16, 1, 0.3, 1) forwards; }
  `]
})
export class TabbedSelectComponent implements OnInit, OnChanges, OnDestroy {
  @Input() items: TabbedSelectItem[] = [];
  @Input() selectedId: number | string | null | undefined = null;
  @Input() placeholder: string = 'اضغط لاختيار صنف...';
  @Input() title: string = 'اختيار من القائمة';
  @Input() defaultIcon: string = 'inventory_2';
  @Input() pageSize: number = 12;
  @Input() allowClear: boolean = true;
  @Input() customTabs?: { key: string; label: string; icon?: string }[];

  @Output() select = new EventEmitter<TabbedSelectItem>();
  @Output() clear = new EventEmitter<void>();
  @Output() selectedIdChange = new EventEmitter<number | string | null>();

  @ViewChild('searchInput') set searchInputRef(el: ElementRef<HTMLInputElement> | undefined) {
    // focus the search box as soon as the modal opens
    if (el && this.isOpen) setTimeout(() => el.nativeElement.focus(), 0);
  }

  isOpen = false;
  searchQuery = '';
  activeTab = 'all';
  currentPage = 1;
  highlightIndex = 0;

  tabs: { key: string; label: string; icon?: string; count: number }[] = [];
  readonly pageSizeOptions: number[] = [12, 24, 48];

  filteredItems: TabbedSelectItem[] = [];
  paginatedItems: TabbedSelectItem[] = [];
  selectedItem: TabbedSelectItem | undefined;

  private index: IndexedItem[] = [];
  private byId = new Map<string, TabbedSelectItem>();
  private searchDebounceTimer: any;
  private lastSignature = '';

  private signature(): string {
    let h = 17;
    for (const it of this.items) {
      h = (h * 31 + (Number(it.id) || 0)) | 0;
      h = (h * 31 + Math.round((it.quantity || 0) * 1000)) | 0;
      h = (h * 31 + Math.round((it.price || 0) * 100)) | 0;
      h = (h * 31 + (it.title ? it.title.length : 0)) | 0;
    }
    return `${this.items.length}:${h}`;
  }

  constructor(private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    if (!this.pageSizeOptions.includes(this.pageSize)) this.pageSize = 12;
    this.lastSignature = this.signature();
    this.rebuildIndex();
    this.resolveSelected();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] || changes['customTabs']) {
      // Parents often pass a freshly-built array on every change detection; skip the
      // (potentially large) re-index when nothing actually changed.
      const sig = this.signature();
      if (sig !== this.lastSignature || changes['customTabs']) {
        this.lastSignature = sig;
        this.rebuildIndex();
        if (this.isOpen) this.applyFilter();
      }
    }
    if (changes['items'] || changes['selectedId']) {
      this.resolveSelected();
    }
  }

  ngOnDestroy(): void {
    if (this.searchDebounceTimer) clearTimeout(this.searchDebounceTimer);
  }

  // ───────── selection ─────────

  private resolveSelected(): void {
    this.selectedItem = (this.selectedId === null || this.selectedId === undefined || this.selectedId === '' || this.selectedId === 0)
      ? undefined
      : this.byId.get(String(this.selectedId));
  }

  isSelected(item: TabbedSelectItem): boolean {
    return this.selectedId !== null && this.selectedId !== undefined && String(item.id) === String(this.selectedId);
  }

  trackById = (_: number, item: TabbedSelectItem) => item.id;

  openModal(): void {
    this.searchQuery = '';
    this.activeTab = 'all';
    this.currentPage = 1;
    this.highlightIndex = 0;
    this.isOpen = true;
    this.applyFilter();
  }

  closeModal(): void {
    this.isOpen = false;
  }

  selectItem(item: TabbedSelectItem): void {
    this.selectedId = item.id;
    this.selectedItem = item;
    this.selectedIdChange.emit(item.id);
    this.select.emit(item);
    this.closeModal();
  }

  clearSelection(): void {
    this.selectedId = null;
    this.selectedItem = undefined;
    this.selectedIdChange.emit(null);
    this.clear.emit();
  }

  // ───────── keyboard ─────────

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen) {
      this.closeModal();
      this.cdr.markForCheck();
    }
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      const target = this.paginatedItems[this.highlightIndex];
      if (target) this.selectItem(target);
    } else if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.highlightIndex = Math.min(this.paginatedItems.length - 1, this.highlightIndex + 1);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.highlightIndex = Math.max(0, this.highlightIndex - 1);
    }
  }

  // ───────── filtering / paging ─────────

  setActiveTab(tabKey: string): void {
    this.activeTab = tabKey;
    this.currentPage = 1;
    this.applyFilter();
  }

  onSearchChange(): void {
    if (this.searchDebounceTimer) clearTimeout(this.searchDebounceTimer);
    this.searchDebounceTimer = setTimeout(() => {
      this.currentPage = 1;
      this.applyFilter();
      this.cdr.markForCheck();
    }, 120);
  }

  setPageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
    this.updatePage();
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.currentPage = 1;
    this.applyFilter();
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.activeTab = 'all';
    this.currentPage = 1;
    this.applyFilter();
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.updatePage();
    }
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.filteredItems.length / this.pageSize));
  }

  get startIndex(): number {
    return (this.currentPage - 1) * this.pageSize;
  }

  get endIndex(): number {
    return Math.min(this.startIndex + this.pageSize, this.filteredItems.length);
  }

  get visiblePages(): number[] {
    const total = this.totalPages;
    let start = Math.max(1, this.currentPage - 2);
    let end = Math.min(total, start + 4);
    start = Math.max(1, end - 4);
    const pages: number[] = [];
    for (let i = start; i <= end; i++) pages.push(i);
    return pages;
  }

  private rebuildIndex(): void {
    // One pass over the items: build search index, id map and tab counts
    this.index = new Array(this.items.length);
    this.byId = new Map();
    const categories = new Map<string, number>();
    let inStock = 0;

    for (let i = 0; i < this.items.length; i++) {
      const it = this.items[i];
      const cat = (it.category || '').trim();
      this.index[i] = {
        item: it,
        cat: cat.toLowerCase(),
        hay: `${it.title || ''} ${it.subtitle || ''} ${it.id} ${cat}`.toLowerCase()
      };
      this.byId.set(String(it.id), it);
      if (cat) categories.set(cat, (categories.get(cat) || 0) + 1);
      if ((it.quantity || 0) > 0) inStock++;
    }

    if (this.customTabs && this.customTabs.length > 0) {
      this.tabs = this.customTabs.map(t => ({
        ...t,
        count: t.key === 'all' ? this.items.length : (categories.get(t.key) || 0)
      }));
      return;
    }

    const tabsList: { key: string; label: string; icon?: string; count: number }[] = [
      { key: 'all', label: 'الكل', icon: 'grid_view', count: this.items.length }
    ];
    if (inStock > 0 && inStock < this.items.length) {
      tabsList.push({ key: 'in_stock', label: 'متوفر بالمخزن', icon: 'check_circle', count: inStock });
    }
    Array.from(categories.entries())
      .sort((a, b) => b[1] - a[1])
      .slice(0, 4)
      .forEach(([cat, count]) => tabsList.push({ key: cat, label: cat, icon: 'category', count }));
    this.tabs = tabsList;
  }

  private applyFilter(): void {
    const q = this.searchQuery.trim().toLowerCase();
    const tab = this.activeTab;
    const tabLower = tab.toLowerCase();
    const out: TabbedSelectItem[] = [];

    for (const entry of this.index) {
      if (tab === 'in_stock') {
        if (!((entry.item.quantity || 0) > 0)) continue;
      } else if (tab !== 'all' && entry.cat !== tabLower) {
        continue;
      }
      if (q && !entry.hay.includes(q)) continue;
      out.push(entry.item);
    }

    this.filteredItems = out;
    this.updatePage();
  }

  private updatePage(): void {
    if (this.currentPage > this.totalPages) this.currentPage = this.totalPages;
    // Only the visible slice is ever rendered, however many items there are
    this.paginatedItems = this.filteredItems.slice(this.startIndex, this.endIndex);
    this.highlightIndex = 0;
  }
}
