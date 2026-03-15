using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MySql.Data.MySqlClient;

namespace ServiceAuto
{
    public partial class MainWindow : Window
    {
        private int _selectedClientID = -1;
        private int _selectedAutoID = -1;
        private int _selectedProgID = -1;

        public MainWindow()
        {
            InitializeComponent();
            DtpProgramare.SelectedDate = DateTime.Today;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!DatabaseHelper.TestConnection())
            {
                TxtDbStatus.Text = "● Deconectat";
                TxtDbStatus.Foreground = (SolidColorBrush)FindResource("Br_Danger");
                MessageBox.Show(
                    "Nu s-a putut conecta la baza de date.\nVerificati ca XAMPP ruleaza si baza de date 'service_auto' exista.",
                    "Eroare conexiune", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            LoadClienti();
            LoadComboClienti();
            LoadAutomobile();
            LoadComboAutomobile();
            LoadProgramari();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        private void ShowPage(int page)
        {
            PageClienti.Visibility = page == 0 ? Visibility.Visible : Visibility.Collapsed;
            PageAuto.Visibility    = page == 1 ? Visibility.Visible : Visibility.Collapsed;
            PageProg.Visibility    = page == 2 ? Visibility.Visible : Visibility.Collapsed;
            BtnNavClienti.Style = (Style)FindResource(page == 0 ? "Btn_NavActive" : "Btn_Nav");
            BtnNavAuto.Style    = (Style)FindResource(page == 1 ? "Btn_NavActive" : "Btn_Nav");
            BtnNavProg.Style    = (Style)FindResource(page == 2 ? "Btn_NavActive" : "Btn_Nav");
        }

        private void NavClienti_Click(object sender, RoutedEventArgs e) { ShowPage(0); LoadClienti(); }
        private void NavAuto_Click(object sender, RoutedEventArgs e) { ShowPage(1); LoadComboClienti(); LoadAutomobile(); }
        private void NavProg_Click(object sender, RoutedEventArgs e) { ShowPage(2); LoadComboAutomobile(); LoadProgramari(); }

        private bool IsPhoneValid(string phone)
        {
            string clean = phone.Replace(" ", "").Replace("-", "");
            if (clean.Length < 7) return false;
            foreach (char c in clean)
                if (!char.IsDigit(c)) return false;
            return true;
        }

        private void ShowErr(TextBlock tb, bool show)
            => tb.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        private void ClearErrs(params TextBlock[] tbs)
        {
            foreach (var tb in tbs) tb.Visibility = Visibility.Collapsed;
        }

        private string GetComboValue(ComboBox cmb)
        {
            if (cmb.SelectedItem is ComboBoxItem cbi) return cbi.Content?.ToString() ?? "";
            return "";
        }

        private int GetComboTag(ComboBox cmb)
        {
            if (cmb.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
                return (int)cbi.Tag;
            return 0;
        }

        // ======== CLIENTI ========

        private void LoadClienti()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string search = TxtSearchClient.Text.Trim();
                    string sql = "SELECT ClientID, Nume, Prenume, Telefon FROM Clienti";
                    if (!string.IsNullOrEmpty(search))
                        sql += " WHERE Nume LIKE @s OR Prenume LIKE @s";
                    sql += " ORDER BY Nume, Prenume";
                    var adapter = new MySqlDataAdapter(sql, conn);
                    if (!string.IsNullOrEmpty(search))
                        adapter.SelectCommand.Parameters.AddWithValue("@s", "%" + search + "%");
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    DgClienti.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Eroare clienti:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadComboClienti()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand(
                        "SELECT ClientID, CONCAT(Nume, ' ', Prenume) FROM Clienti ORDER BY Nume", conn);
                    var reader = cmd.ExecuteReader();
                    CmbClientAuto.Items.Clear();
                    CmbClientAuto.Items.Add(new ComboBoxItem { Content = "— selecteaza client —", Tag = 0 });
                    while (reader.Read())
                        CmbClientAuto.Items.Add(new ComboBoxItem { Content = reader.GetString(1), Tag = reader.GetInt32(0) });
                    reader.Close();
                    CmbClientAuto.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void DgClienti_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgClienti.SelectedItem is not DataRowView row) return;
            _selectedClientID = Convert.ToInt32(row["ClientID"]);
            TxtNume.Text = row["Nume"].ToString();
            TxtPrenume.Text = row["Prenume"].ToString();
            TxtTelefon.Text = row["Telefon"].ToString();
        }

        private void TxtSearchClient_Changed(object sender, TextChangedEventArgs e) => LoadClienti();

        private bool ValidateClient()
        {
            ClearErrs(ErrNume, ErrPrenume, ErrTelefon);
            bool ok = true;
            if (string.IsNullOrWhiteSpace(TxtNume.Text)) { ShowErr(ErrNume, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtPrenume.Text)) { ShowErr(ErrPrenume, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtTelefon.Text)) { ErrTelefon.Text = "Camp obligatoriu"; ShowErr(ErrTelefon, true); ok = false; }
            else if (!IsPhoneValid(TxtTelefon.Text)) { ErrTelefon.Text = "Doar cifre, minim 7 caractere"; ShowErr(ErrTelefon, true); ok = false; }
            return ok;
        }

        private void BtnAdaugaClient_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateClient()) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("INSERT INTO Clienti (Nume, Prenume, Telefon) VALUES (@n,@p,@t)", conn);
                    cmd.Parameters.AddWithValue("@n", TxtNume.Text.Trim());
                    cmd.Parameters.AddWithValue("@p", TxtPrenume.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", TxtTelefon.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
                ClearClientForm(); LoadClienti(); LoadComboClienti();
            }
            catch (Exception ex) { MessageBox.Show("Eroare adaugare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnModificaClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClientID == -1) { MessageBox.Show("Selectati un client din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!ValidateClient()) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("UPDATE Clienti SET Nume=@n,Prenume=@p,Telefon=@t WHERE ClientID=@id", conn);
                    cmd.Parameters.AddWithValue("@n", TxtNume.Text.Trim());
                    cmd.Parameters.AddWithValue("@p", TxtPrenume.Text.Trim());
                    cmd.Parameters.AddWithValue("@t", TxtTelefon.Text.Trim());
                    cmd.Parameters.AddWithValue("@id", _selectedClientID);
                    cmd.ExecuteNonQuery();
                }
                ClearClientForm(); LoadClienti(); LoadComboClienti();
            }
            catch (Exception ex) { MessageBox.Show("Eroare modificare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnStergeClient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClientID == -1) { MessageBox.Show("Selectati un client din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("Stergeti clientul selectat?\nSe vor sterge si automobilele si programarile asociate.",
                "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    new MySqlCommand("DELETE FROM Clienti WHERE ClientID=@id", conn)
                        .Parameters.AddWithValue("@id", _selectedClientID);
                    var cmd = new MySqlCommand("DELETE FROM Clienti WHERE ClientID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedClientID);
                    cmd.ExecuteNonQuery();
                }
                ClearClientForm(); LoadClienti(); LoadComboClienti();
            }
            catch (Exception ex) { MessageBox.Show("Eroare stergere:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ClearClientForm()
        {
            TxtNume.Clear(); TxtPrenume.Clear(); TxtTelefon.Clear();
            ClearErrs(ErrNume, ErrPrenume, ErrTelefon);
            _selectedClientID = -1;
            DgClienti.SelectedItem = null;
        }

        // ======== AUTOMOBILE ========

        private void LoadAutomobile()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT a.AutomobilID, CONCAT(c.Nume,' ',c.Prenume) AS Client,
                                   a.Marca, a.Model, a.NumarInmatriculare
                                   FROM Automobile a INNER JOIN Clienti c ON a.ClientID=c.ClientID
                                   ORDER BY a.AutomobilID";
                    var adapter = new MySqlDataAdapter(sql, conn);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    DgAuto.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Eroare automobile:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void LoadComboAutomobile()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT a.AutomobilID, CONCAT(a.Marca,' ',a.Model,' (',a.NumarInmatriculare,') — ',c.Nume) AS Label
                                   FROM Automobile a INNER JOIN Clienti c ON a.ClientID=c.ClientID ORDER BY a.Marca";
                    var cmd = new MySqlCommand(sql, conn);
                    var reader = cmd.ExecuteReader();
                    CmbAutoProg.Items.Clear();
                    CmbAutoProg.Items.Add(new ComboBoxItem { Content = "— selecteaza automobil —", Tag = 0 });
                    while (reader.Read())
                        CmbAutoProg.Items.Add(new ComboBoxItem { Content = reader.GetString(1), Tag = reader.GetInt32(0) });
                    reader.Close();
                    CmbAutoProg.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void DgAuto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgAuto.SelectedItem is not DataRowView row) return;
            _selectedAutoID = Convert.ToInt32(row["AutomobilID"]);
            TxtMarca.Text = row["Marca"].ToString();
            TxtModel.Text = row["Model"].ToString();
            TxtNrInmatriculare.Text = row["NumarInmatriculare"].ToString();
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT ClientID FROM Automobile WHERE AutomobilID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedAutoID);
                    int clientId = Convert.ToInt32(cmd.ExecuteScalar());
                    foreach (ComboBoxItem item in CmbClientAuto.Items)
                        if (item.Tag is int t && t == clientId) { CmbClientAuto.SelectedItem = item; break; }
                }
            }
            catch { }
        }

        private bool ValidateAuto()
        {
            ClearErrs(ErrClientAuto, ErrMarca, ErrModel, ErrNr);
            bool ok = true;
            if (GetComboTag(CmbClientAuto) == 0) { ShowErr(ErrClientAuto, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtMarca.Text)) { ShowErr(ErrMarca, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtModel.Text)) { ShowErr(ErrModel, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtNrInmatriculare.Text)) { ShowErr(ErrNr, true); ok = false; }
            return ok;
        }

        private void BtnAdaugaAuto_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAuto()) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("INSERT INTO Automobile (ClientID,Marca,Model,NumarInmatriculare) VALUES (@c,@m,@mo,@nr)", conn);
                    cmd.Parameters.AddWithValue("@c", GetComboTag(CmbClientAuto));
                    cmd.Parameters.AddWithValue("@m", TxtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@mo", TxtModel.Text.Trim());
                    cmd.Parameters.AddWithValue("@nr", TxtNrInmatriculare.Text.Trim());
                    cmd.ExecuteNonQuery();
                }
                ClearAutoForm(); LoadAutomobile(); LoadComboAutomobile();
            }
            catch (Exception ex) { MessageBox.Show("Eroare adaugare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnModificaAuto_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAutoID == -1) { MessageBox.Show("Selectati un automobil din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!ValidateAuto()) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("UPDATE Automobile SET ClientID=@c,Marca=@m,Model=@mo,NumarInmatriculare=@nr WHERE AutomobilID=@id", conn);
                    cmd.Parameters.AddWithValue("@c", GetComboTag(CmbClientAuto));
                    cmd.Parameters.AddWithValue("@m", TxtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@mo", TxtModel.Text.Trim());
                    cmd.Parameters.AddWithValue("@nr", TxtNrInmatriculare.Text.Trim());
                    cmd.Parameters.AddWithValue("@id", _selectedAutoID);
                    cmd.ExecuteNonQuery();
                }
                ClearAutoForm(); LoadAutomobile(); LoadComboAutomobile();
            }
            catch (Exception ex) { MessageBox.Show("Eroare modificare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnStergeAuto_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAutoID == -1) { MessageBox.Show("Selectati un automobil din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("Stergeti automobilul selectat?\nSe vor sterge si programarile asociate.",
                "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM Automobile WHERE AutomobilID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedAutoID);
                    cmd.ExecuteNonQuery();
                }
                ClearAutoForm(); LoadAutomobile(); LoadComboAutomobile();
            }
            catch (Exception ex) { MessageBox.Show("Eroare stergere:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ClearAutoForm()
        {
            CmbClientAuto.SelectedIndex = 0;
            TxtMarca.Clear(); TxtModel.Clear(); TxtNrInmatriculare.Clear();
            ClearErrs(ErrClientAuto, ErrMarca, ErrModel, ErrNr);
            _selectedAutoID = -1;
            DgAuto.SelectedItem = null;
        }

        // ======== PROGRAMARI ========

        private void LoadProgramari()
        {
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    string filterStatus = CmbFilterStatus.SelectedIndex > 0 ? GetComboValue(CmbFilterStatus) : "";
                    string sql = @"SELECT p.ProgramareID, CONCAT(a.Marca,' ',a.Model,' (',a.NumarInmatriculare,')') AS Automobil,
                                   p.DataProgramarii, p.TipServiciu, p.StatusProgramare
                                   FROM Programari p INNER JOIN Automobile a ON p.AutomobilID=a.AutomobilID";
                    if (!string.IsNullOrEmpty(filterStatus))
                        sql += " WHERE p.StatusProgramare=@s";
                    sql += " ORDER BY p.DataProgramarii DESC";
                    var adapter = new MySqlDataAdapter(sql, conn);
                    if (!string.IsNullOrEmpty(filterStatus))
                        adapter.SelectCommand.Parameters.AddWithValue("@s", filterStatus);
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    DgProg.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex) { MessageBox.Show("Eroare programari:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void DgProg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgProg.SelectedItem is not DataRowView row) return;
            _selectedProgID = Convert.ToInt32(row["ProgramareID"]);
            DtpProgramare.SelectedDate = Convert.ToDateTime(row["DataProgramarii"]);
            TxtTipServiciu.Text = row["TipServiciu"].ToString();
            string status = row["StatusProgramare"].ToString() ?? "";
            foreach (ComboBoxItem item in CmbStatusProg.Items)
                if (item.Content?.ToString() == status) { CmbStatusProg.SelectedItem = item; break; }
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT AutomobilID FROM Programari WHERE ProgramareID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedProgID);
                    int autoId = Convert.ToInt32(cmd.ExecuteScalar());
                    foreach (ComboBoxItem item in CmbAutoProg.Items)
                        if (item.Tag is int t && t == autoId) { CmbAutoProg.SelectedItem = item; break; }
                }
            }
            catch { }
        }

        private void CmbFilterStatus_Changed(object sender, SelectionChangedEventArgs e) => LoadProgramari();

        private bool ValidateProg()
        {
            ClearErrs(ErrAutoProg, ErrData, ErrTip);
            bool ok = true;
            if (GetComboTag(CmbAutoProg) == 0) { ShowErr(ErrAutoProg, true); ok = false; }
            if (DtpProgramare.SelectedDate == null) { ErrData.Text = "Data este obligatorie"; ShowErr(ErrData, true); ok = false; }
            else if (DtpProgramare.SelectedDate.Value.Date < DateTime.Today) { ErrData.Text = "Data nu poate fi in trecut"; ShowErr(ErrData, true); ok = false; }
            if (string.IsNullOrWhiteSpace(TxtTipServiciu.Text)) { ShowErr(ErrTip, true); ok = false; }
            return ok;
        }

        private void BtnAdaugaProg_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateProg()) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("INSERT INTO Programari (AutomobilID,DataProgramarii,TipServiciu,StatusProgramare) VALUES (@a,@d,@t,@s)", conn);
                    cmd.Parameters.AddWithValue("@a", GetComboTag(CmbAutoProg));
                    cmd.Parameters.AddWithValue("@d", DtpProgramare.SelectedDate!.Value.Date);
                    cmd.Parameters.AddWithValue("@t", TxtTipServiciu.Text.Trim());
                    cmd.Parameters.AddWithValue("@s", GetComboValue(CmbStatusProg));
                    cmd.ExecuteNonQuery();
                }
                ClearProgForm(); LoadProgramari();
            }
            catch (Exception ex) { MessageBox.Show("Eroare adaugare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnModificaProg_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProgID == -1) { MessageBox.Show("Selectati o programare din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (!ValidateProg()) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("UPDATE Programari SET AutomobilID=@a,DataProgramarii=@d,TipServiciu=@t,StatusProgramare=@s WHERE ProgramareID=@id", conn);
                    cmd.Parameters.AddWithValue("@a", GetComboTag(CmbAutoProg));
                    cmd.Parameters.AddWithValue("@d", DtpProgramare.SelectedDate!.Value.Date);
                    cmd.Parameters.AddWithValue("@t", TxtTipServiciu.Text.Trim());
                    cmd.Parameters.AddWithValue("@s", GetComboValue(CmbStatusProg));
                    cmd.Parameters.AddWithValue("@id", _selectedProgID);
                    cmd.ExecuteNonQuery();
                }
                ClearProgForm(); LoadProgramari();
            }
            catch (Exception ex) { MessageBox.Show("Eroare modificare:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnStergeProg_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProgID == -1) { MessageBox.Show("Selectati o programare din tabel.", "Atentie", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show("Stergeti programarea selectata?",
                "Confirmare", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                using (var conn = DatabaseHelper.GetConnection())
                {
                    conn.Open();
                    var cmd = new MySqlCommand("DELETE FROM Programari WHERE ProgramareID=@id", conn);
                    cmd.Parameters.AddWithValue("@id", _selectedProgID);
                    cmd.ExecuteNonQuery();
                }
                ClearProgForm(); LoadProgramari();
            }
            catch (Exception ex) { MessageBox.Show("Eroare stergere:\n" + ex.Message, "Eroare", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ClearProgForm()
        {
            CmbAutoProg.SelectedIndex = 0;
            DtpProgramare.SelectedDate = DateTime.Today;
            TxtTipServiciu.Clear();
            CmbStatusProg.SelectedIndex = 0;
            ClearErrs(ErrAutoProg, ErrData, ErrTip);
            _selectedProgID = -1;
            DgProg.SelectedItem = null;
        }
    }
}
