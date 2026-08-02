using System.Drawing;
using System.Windows.Forms;
using ImtiazQueueSimulator.Models;
using ImtiazQueueSimulator.Statistics;

namespace ImtiazQueueSimulator.Forms
{
    /// <summary>
    /// Searchable, sortable, filterable customer records table.
    /// Features:
    ///   - Fixed column widths & proper alignment (numbers right/center aligned)
    ///   - Status badges (🟢 Completed, 🔵 InService, 🟡 Waiting, ⚪ Pending)
    ///   - Empty state label before simulation
    ///   - Longest waiting customer panel at bottom
    /// </summary>
    public class CustomerRecordsPanel : UserControl
    {
        private DataGridView _grid = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cmbStatusFilter = null!;
        private ComboBox _cmbServerFilter = null!;
        private Label _lblCount = null!;
        private Panel _topWaitPanel = null!;
        private Label _emptyLabel = null!;
        private List<Customer> _allCustomers = new();
        private List<Customer> _filteredCustomers = new();

        public CustomerRecordsPanel()
        {
            BackColor = Color.FromArgb(244, 246, 250);
            AutoScroll = false;
            BuildUI();
        }

        private void BuildUI()
        {
            // Title
            var title = new Label
            {
                Text      = "👥 CUSTOMER RECORDS",
                Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(15, 12)
            };
            Controls.Add(title);

            // Filter Toolbar Panel (Clean white background, 56px height)
            int searchY = title.Bottom + 10;
            var searchPanel = new Panel
            {
                Location  = new Point(15, searchY),
                Size      = new Size(Width - 30, 56),
                BackColor = Color.White,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            searchPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, searchPanel.Width - 1, searchPanel.Height - 1);
            };
            Controls.Add(searchPanel);

            var toolbarFlow = new FlowLayoutPanel
            {
                Location      = new Point(0, 0),
                Size          = new Size(Math.Max(300, searchPanel.Width - 150), 56),
                Anchor        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                BackColor     = Color.Transparent,
                Padding       = new Padding(16, 8, 16, 8)
            };
            searchPanel.Controls.Add(toolbarFlow);

            // 1. Search Box Container (Border #CFD5DD, radius 7px, 40px height, min-width 260px)
            var searchBox = new Panel
            {
                Size      = new Size(320, 40),
                BackColor = Color.White,
                Margin    = new Padding(0, 0, 28, 0) // 28px gap between Search and Status
            };
            searchBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(207, 213, 221), 1.2f);
                var r = new Rectangle(0, 0, searchBox.Width - 1, searchBox.Height - 1);
                DrawRoundedRectangle(e.Graphics, pen, r, 7);
            };
            toolbarFlow.Controls.Add(searchBox);

            // Search Icon positioned inside input at left: 14px (width: 20px)
            var searchIcon = new Label
            {
                Text      = "🔍",
                Font      = new Font("Segoe UI Symbol", 10f),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize  = false,
                Size      = new Size(20, 20),
                Location  = new Point(14, 10)
            };
            searchBox.Controls.Add(searchIcon);

            // Search TextBox starts at X = 48px (padding-left: 48px). Icon ends at 34px, 14px gap, text starts at 48px.
            _txtSearch = new TextBox
            {
                Font            = new Font("Segoe UI", 10f),
                Location        = new Point(48, 9),
                Size            = new Size(searchBox.Width - 60, 22),
                BorderStyle     = BorderStyle.None,
                PlaceholderText = "Search customer...",
                Anchor          = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilters();
            searchBox.Controls.Add(_txtSearch);

            // 2. Status Filter Group (Label + Dropdown together, 28px gap to Server)
            var statusGroup = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 2, 28, 0) // 28px gap between Status and Server
            };
            toolbarFlow.Controls.Add(statusGroup);

            var statusLabel = new Label
            {
                Text      = "Status:",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Margin    = new Padding(0, 7, 8, 0) // 8px gap between label and dropdown
            };
            statusGroup.Controls.Add(statusLabel);

            _cmbStatusFilter = new ComboBox
            {
                Font          = new Font("Segoe UI", 9.5f),
                Size          = new Size(120, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin        = new Padding(0, 3, 0, 0)
            };
            _cmbStatusFilter.Items.AddRange(new[] { "All", "Waiting", "InService", "Completed" });
            _cmbStatusFilter.SelectedIndex = 0;
            _cmbStatusFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            statusGroup.Controls.Add(_cmbStatusFilter);

            // 3. Server Filter Group (Label + Dropdown together)
            var serverGroup = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                BackColor     = Color.Transparent,
                Margin        = new Padding(0, 2, 0, 0)
            };
            toolbarFlow.Controls.Add(serverGroup);

            var serverLabel = new Label
            {
                Text      = "Server:",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Margin    = new Padding(0, 7, 8, 0) // 8px gap between label and dropdown
            };
            serverGroup.Controls.Add(serverLabel);

            _cmbServerFilter = new ComboBox
            {
                Font          = new Font("Segoe UI", 9.5f),
                Size          = new Size(130, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin        = new Padding(0, 3, 0, 0)
            };
            _cmbServerFilter.Items.Add("All Servers");
            _cmbServerFilter.SelectedIndex = 0;
            _cmbServerFilter.SelectedIndexChanged += (s, e) => ApplyFilters();
            serverGroup.Controls.Add(_cmbServerFilter);

            // 4. Customer count label (right-aligned)
            _lblCount = new Label
            {
                Text      = "Showing: 0 / 0",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(searchPanel.Width - 140, 18),
                Anchor    = AnchorStyles.Top | AnchorStyles.Right
            };
            searchPanel.Controls.Add(_lblCount);

            int contentY = searchY + 62;

            // Empty state label sits cleanly above grid
            _emptyLabel = new Label
            {
                Text      = "No customer records yet. Start a simulation to generate customer records.",
                Font      = new Font("Segoe UI Semibold", 9.5f),
                ForeColor = Color.FromArgb(71, 85, 105),
                AutoSize  = true,
                Location  = new Point(15, contentY + 4),
                Visible   = true
            };
            Controls.Add(_emptyLabel);

            int gridY = contentY + 32;

            // Grid sits cleanly below empty state label
            _grid = new DataGridView
            {
                Location = new Point(15, gridY),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9f),
                RowTemplate = { Height = 32 },
                GridColor = Color.FromArgb(241, 245, 249),
                DefaultCellStyle =
                {
                    SelectionBackColor = Color.FromArgb(239, 246, 255),
                    SelectionForeColor = Color.FromArgb(30, 41, 59)
                },
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersDefaultCellStyle =
                {
                    BackColor = Color.FromArgb(15, 23, 42),      // Dark Navy Slate 900
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Bold", 9.5f),
                    Padding = new Padding(6, 0, 6, 0)
                },
                EnableHeadersVisualStyles = false
            };

            Resize += (s, e) =>
            {
                searchPanel.Width = Math.Max(300, Width - 30);
                toolbarFlow.Width = Math.Max(250, searchPanel.Width - 150);
                int neededH = Math.Max(56, toolbarFlow.PreferredSize.Height + 12);
                searchPanel.Height = neededH;
                _lblCount.Location = new Point(searchPanel.Width - _lblCount.PreferredWidth - 16, 18);
                _emptyLabel.Location = new Point(15, searchPanel.Bottom + 10);
                int newGridY = searchPanel.Bottom + 36;
                _grid.Location = new Point(15, newGridY);
                _grid.Size = new Size(Math.Max(300, Width - 30), Math.Max(100, Height - newGridY - 15));
            };

            _grid.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "ID",        HeaderText = "ID",        Width = 55, HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "Name",      HeaderText = "Name",      Width = 140 },
                new DataGridViewTextBoxColumn { Name = "Arrival",   HeaderText = "Arrival",   Width = 85,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "SvcStart",  HeaderText = "Svc Start", Width = 85,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "SvcTime",   HeaderText = "Service",   Width = 80,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "Departure", HeaderText = "Departure", Width = 85,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "Wq",        HeaderText = "Wq",        Width = 80,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "W",         HeaderText = "W",         Width = 80,  HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter } }, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter } },
                new DataGridViewTextBoxColumn { Name = "Server",    HeaderText = "Server",    Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Status",    HeaderText = "Status",    Width = 100 },
                new DataGridViewButtonColumn  { Name = "View",      HeaderText = "Action",    Text = "VIEW", UseColumnTextForButtonValue = true, Width = 70 }
            });
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _grid.CellClick += Grid_CellClick;
            _grid.ColumnHeaderMouseClick += Grid_ColumnHeaderClick;
            Controls.Add(_grid);

            // Longest waiting panel (Clean 200px height card container)
            _topWaitPanel = new Panel
            {
                Anchor    = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Height    = 200,
                BackColor = Color.White
            };
            _topWaitPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(226, 232, 240), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, _topWaitPanel.Width - 1, _topWaitPanel.Height - 1);
            };
            Controls.Add(_topWaitPanel);

            var waitHeader = new Label
            {
                Text      = "⏳ LONGEST WAITING CUSTOMERS",
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize  = true,
                Location  = new Point(16, 10)
            };
            _topWaitPanel.Controls.Add(waitHeader);

            var waitDivider = new Panel
            {
                Location  = new Point(16, 36),
                Size      = new Size(_topWaitPanel.Width - 32, 1),
                BackColor = Color.FromArgb(226, 232, 240),
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _topWaitPanel.Controls.Add(waitDivider);

            _waitFlowPanel = new FlowLayoutPanel
            {
                Location      = new Point(16, 42),
                Size          = new Size(_topWaitPanel.Width - 32, 146),
                Anchor        = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = Color.Transparent
            };
            _topWaitPanel.Controls.Add(_waitFlowPanel);

            Resize += (s, e) => LayoutControls();
            LayoutControls();
        }

        private FlowLayoutPanel _waitFlowPanel = null!;

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            int d = radius * 2;
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(pen, path);
        }

        private void LayoutControls()
        {
            int bottomH = 200;
            int startY  = 138;
            _topWaitPanel.Location = new Point(15, Height - bottomH - 12);
            _topWaitPanel.Size     = new Size(Width - 30, bottomH);
            _grid.Size             = new Size(Width - 30, Math.Max(120, Height - startY - bottomH - 24));

            if (_waitFlowPanel != null)
            {
                int itemW = Math.Max(200, _waitFlowPanel.ClientSize.Width - 6);
                foreach (Control c in _waitFlowPanel.Controls)
                {
                    c.Width = Math.Max(200, itemW);
                    foreach (Control child in c.Controls)
                    {
                        if (child.Name == "lblTime")
                        {
                            child.Location = new Point(c.Width - child.PreferredSize.Width - 14, 6);
                        }
                    }
                }
            }
        }

        public void LoadCustomers(List<Customer> customers)
        {
            _allCustomers = customers;
            _emptyLabel.Visible = customers.Count == 0;

            var servers = customers.Select(c => c.AssignedServer).Where(s => s > 0).Distinct().OrderBy(s => s).ToList();
            _cmbServerFilter.Items.Clear();
            _cmbServerFilter.Items.Add("All Servers");
            foreach (var s in servers)
                _cmbServerFilter.Items.Add($"Cashier {s:D2}");
            _cmbServerFilter.SelectedIndex = 0;

            ApplyFilters();
            UpdateTopWaiting();
        }

        private void ApplyFilters()
        {
            string search = _txtSearch.Text.Trim().ToLower();
            string status = _cmbStatusFilter.SelectedItem?.ToString() ?? "All";
            string server = _cmbServerFilter.SelectedItem?.ToString() ?? "All Servers";

            _filteredCustomers = _allCustomers.Where(c =>
            {
                if (!string.IsNullOrEmpty(search))
                {
                    if (!c.Name.ToLower().Contains(search) && !c.Id.ToString().Contains(search))
                        return false;
                }
                if (status != "All" && c.Status != status) return false;
                if (server != "All Servers" && $"Cashier {c.AssignedServer:D2}" != server) return false;
                return true;
            }).ToList();

            PopulateGrid();
        }

        private void PopulateGrid()
        {
            _grid.Rows.Clear();
            _lblCount.Text = $"Showing: {_filteredCustomers.Count} / {_allCustomers.Count}";

            foreach (var c in _filteredCustomers)
            {
                int rowIdx = _grid.Rows.Add(
                    $"{c.Id:D3}",
                    c.Name,
                    Customer.FormatTime(c.ArrivalTime),
                    Customer.FormatTime(c.QueueEntryTime),
                    Customer.FormatTime(c.ServiceStartTime),
                    Customer.FormatTime(c.DepartureTime),
                    Customer.FormatDuration(c.WaitingTime),
                    Customer.FormatDuration(c.TimeInSystem),
                    c.AssignedServer > 0 ? $"Cashier {c.AssignedServer:D2}" : "—",
                    c.Status,
                    "Details"
                );

                var row = _grid.Rows[rowIdx];
                Color statusColor = c.Status switch
                {
                    "Completed" => Color.FromArgb(22, 163, 74),
                    "InService" => Color.FromArgb(37, 99, 235),
                    "Waiting"   => Color.FromArgb(217, 119, 6),
                    _           => Color.FromArgb(148, 163, 184)
                };
                row.Cells[9].Style.ForeColor = statusColor;
                row.Cells[9].Style.Font = new Font("Segoe UI Semibold", 8.5f);
                row.Tag = c;
            }
        }

        private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (_grid.Columns[e.ColumnIndex].Name == "View" || e.ColumnIndex == _grid.Columns.Count - 1)
            {
                if (_grid.Rows[e.RowIndex].Tag is Customer customer)
                {
                    var detailForm = new CustomerDetailForm(customer);
                    detailForm.ShowDialog();
                }
            }
        }

        private bool _sortAscending = true;
        private int _lastSortColumn = -1;

        private void Grid_ColumnHeaderClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex == _lastSortColumn)
                _sortAscending = !_sortAscending;
            else
            {
                _lastSortColumn = e.ColumnIndex;
                _sortAscending = true;
            }

            _filteredCustomers = e.ColumnIndex switch
            {
                0 => _sortAscending ? _filteredCustomers.OrderBy(c => c.Id).ToList() : _filteredCustomers.OrderByDescending(c => c.Id).ToList(),
                2 => _sortAscending ? _filteredCustomers.OrderBy(c => c.ArrivalTime).ToList() : _filteredCustomers.OrderByDescending(c => c.ArrivalTime).ToList(),
                5 => _sortAscending ? _filteredCustomers.OrderBy(c => c.DepartureTime).ToList() : _filteredCustomers.OrderByDescending(c => c.DepartureTime).ToList(),
                6 => _sortAscending ? _filteredCustomers.OrderBy(c => c.WaitingTime).ToList() : _filteredCustomers.OrderByDescending(c => c.WaitingTime).ToList(),
                7 => _sortAscending ? _filteredCustomers.OrderBy(c => c.TimeInSystem).ToList() : _filteredCustomers.OrderByDescending(c => c.TimeInSystem).ToList(),
                8 => _sortAscending ? _filteredCustomers.OrderBy(c => c.AssignedServer).ToList() : _filteredCustomers.OrderByDescending(c => c.AssignedServer).ToList(),
                _ => _filteredCustomers
            };

            PopulateGrid();
        }

        private void UpdateTopWaiting()
        {
            if (_waitFlowPanel == null) return;
            _waitFlowPanel.Controls.Clear();

            var top = QueueStatistics.GetLongestWaiting(_allCustomers, 10);
            int itemW = Math.Max(200, _waitFlowPanel.ClientSize.Width - 6);

            for (int i = 0; i < top.Count; i++)
            {
                var c = top[i];
                var rowItem = new Panel
                {
                    Size      = new Size(itemW, 32),
                    BackColor = i % 2 == 0 ? Color.FromArgb(248, 250, 252) : Color.White,
                    Margin    = new Padding(0, 0, 0, 4),
                    Cursor    = Cursors.Hand,
                    Tag       = c
                };

                rowItem.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(241, 245, 249), 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, rowItem.Width - 1, rowItem.Height - 1);
                };

                var lblRank = new Label
                {
                    Text        = $"{i + 1}.",
                    Font        = new Font("Segoe UI Semibold", 9.5f),
                    ForeColor   = Color.FromArgb(71, 85, 105),
                    AutoSize    = false,
                    Size        = new Size(28, 20),
                    Location    = new Point(8, 6),
                    UseMnemonic = false
                };
                rowItem.Controls.Add(lblRank);

                var lblName = new Label
                {
                    Text        = c.Name,
                    Font        = new Font("Segoe UI Semibold", 9.5f),
                    ForeColor   = Color.FromArgb(30, 41, 59),
                    AutoSize    = true,
                    Location    = new Point(36, 6),
                    UseMnemonic = false
                };
                rowItem.Controls.Add(lblName);

                var lblTime = new Label
                {
                    Text        = Customer.FormatDuration(c.WaitingTime),
                    Font        = new Font("Segoe UI Bold", 9.5f),
                    ForeColor   = Color.FromArgb(220, 38, 38),
                    AutoSize    = true,
                    UseMnemonic = false,
                    Name        = "lblTime"
                };
                lblTime.Location = new Point(rowItem.Width - lblTime.PreferredSize.Width - 14, 6);
                lblTime.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
                rowItem.Controls.Add(lblTime);

                void OpenDetail(object? s, EventArgs e)
                {
                    new CustomerDetailForm(c).ShowDialog();
                }

                rowItem.Click += OpenDetail;
                lblRank.Click += OpenDetail;
                lblName.Click += OpenDetail;
                lblTime.Click += OpenDetail;

                _waitFlowPanel.Controls.Add(rowItem);
            }
        }
    }
}
