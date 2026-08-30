using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace DMXServer
{
    public partial class SetlistForm : Form
    {
        public SetlistForm()
        {
            InitializeComponent();
        }

        private MainForm mainForm = null;
        public SetlistForm(Form callingForm)
        {
            mainForm = callingForm as MainForm;
            InitializeComponent();

            ReadSetlistXML(mainForm.currentShow);
            FillLBAvailable();
            FillLBAllocated();
            // show selected available item's data in the text boxes
            this.lbAvailable.SelectedIndexChanged += lbAvailable_SelectedIndexChanged;

        }

        private void ReadSetlistXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + mainForm.setlistFile);

            mainForm.setlistMappings = (from S in root.Elements("song")
                               select S).ToList();

        }

        private void FillLBAvailable()
        {
            try
            {
                var mappings = from M in mainForm.setlistMappings
                               where M.Element("position").Value == ""
                               select M;


                lbAvailable.Items.Clear();

                foreach (XElement mapping in mappings.OrderBy(a => a.Element("name").Value))
                {
                        lbAvailable.Items.Add(mapping.Element("name").Value);
                }
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void FillLBAllocated()
        {
            try
            {
                var mappings = from M in mainForm.setlistMappings
                               where M.Element("position").Value != ""
                               orderby int.Parse(M.Element("position").Value)
                               select M;


                clbAllocated.Items.Clear();
                int i = 1;

                foreach (XElement mapping in mappings)
                {
                    var cont = mapping.Element("continue");
                    var chk = cont != null && cont.Value == "1"; 
                    clbAllocated.Items.Add(i++.ToString() + ". " + mapping.Element("name").Value,chk);
                }
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SetlistForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            UpdateMappings();

            var result = MessageBox.Show(this, "Save changes to disk?", "Save Midi Mapping changes", MessageBoxButtons.YesNoCancel);

            switch (result)
            {
                case (DialogResult.Cancel):
                    e.Cancel = true;
                    break;
                case (DialogResult.Yes):
                    SaveMappings(mainForm.currentShow);
                    break;
                case (DialogResult.No):
                    break;
            }
        }

        private void SaveMappings(string show)
        {
            try
            {
                XElement root = new XElement("setlist");
                root.Add(mainForm.setlistMappings);
                root.Save(show + ".xml\\" + mainForm.setlistFile);
                mainForm.OutputText("Changes saved!");
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (clbAllocated.SelectedItem == null)
                return;

            var item = clbAllocated.SelectedItem.ToString();

            var name = item.Substring(item.IndexOf('.') + 2);

            var song = mainForm.setlistMappings.First(a => a.Element("name").Value == name);

            song.Element("position").Value = "";

            FillLBAvailable();
            FillLBAllocated();
            UpdateMappings();

        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (lbAvailable.SelectedItem == null)
                return;

            var item = lbAvailable.SelectedItem.ToString();

            var allocated = clbAllocated.Items.Count;

            var song = mainForm.setlistMappings.First(a => a.Element("name").Value == item);

            song.Element("position").Value = (allocated + 1).ToString();

            FillLBAvailable();
            FillLBAllocated();
            UpdateMappings();

            clbAllocated.SelectedItem = clbAllocated.Items[clbAllocated.Items.Count - 1];
            // focus the available list so user can continue adding
            lbAvailable.Focus();
            
        }

        private void UpdateMappings()
        {
            foreach (var item in mainForm.setlistMappings)
                item.Element("position").Value = "";

            int i = 0;

            foreach(var item in clbAllocated.Items)
            {
                
                var index = item.ToString().Substring(0, item.ToString().IndexOf('.'));

                var name = item.ToString().Substring(item.ToString().IndexOf('.') + 2);

                var song = mainForm.setlistMappings.First(a => a.Element("name").Value == name);

                song.Element("position").Value = index;


                var ckd = clbAllocated.GetItemChecked(i++);

                if (ckd)
                    song.Element("continue").Value = "1";
                else
                    song.Element("continue").Value = "0";


            }
        }

        private void btnUp_Click(object sender, EventArgs e)
        {
            if (clbAllocated.SelectedItem == null)
                return;

            var itemIndex = clbAllocated.SelectedIndex;
            if (itemIndex == 0)
                return;

            string item = clbAllocated.SelectedItem.ToString();

            var name = item.ToString().Substring(item.ToString().IndexOf('.') + 2);
      
            var itemAbove = clbAllocated.Items[itemIndex - 1];

            var nameAbove = itemAbove.ToString().Substring(itemAbove.ToString().IndexOf('.') + 2);

            var temp = clbAllocated.SelectedItem;

            clbAllocated.Items[itemIndex] = (itemIndex + 1).ToString() + ". " + nameAbove;

            clbAllocated.Items[itemIndex - 1] = (itemIndex).ToString() + ". " + name;

            clbAllocated.SelectedIndex--;

            UpdateMappings();
        }

        private void btnDown_Click(object sender, EventArgs e)
        {
            if (clbAllocated.SelectedItem == null)
                return;

            var itemIndex = clbAllocated.SelectedIndex;
            if (itemIndex >= clbAllocated.Items.Count - 1)
                return;

            string item = clbAllocated.SelectedItem.ToString();

            var name = item.ToString().Substring(item.ToString().IndexOf('.') + 2);

            var itemBelow = clbAllocated.Items[itemIndex + 1];

            var nameBelow = itemBelow.ToString().Substring(itemBelow.ToString().IndexOf('.') + 2);

            var temp = clbAllocated.SelectedItem;

            clbAllocated.Items[itemIndex] = (itemIndex + 1).ToString() + ". " + nameBelow;

            clbAllocated.Items[itemIndex + 1] = (itemIndex + 2).ToString() + ". " + name;

            clbAllocated.SelectedIndex++;

            UpdateMappings();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            foreach (var item in mainForm.setlistMappings)
            {
                item.Element("position").Value = "";
                item.Element("continue").Value = "0";
            }

            FillLBAvailable();
            FillLBAllocated();

        }

        private void btnAddNew_Click(object sender, EventArgs e)
        {
            try
            {
                var name = txtNewName.Text.Trim();
                var osc = txtNewOSC.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show(this, "Please enter a name for the new item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (mainForm.setlistMappings.Any(s => string.Equals(s.Element("name").Value, name, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show(this, "An item with that name already exists.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var song = new XElement("song",
                    new XElement("name", name),
                    new XElement("position", ""),
                    new XElement("continue", "0"),
                    new XElement("oscaddress", osc)
                );

                mainForm.setlistMappings.Add(song);
                FillLBAvailable();

                // select the newly added item in the available list and focus it
                var idx = lbAvailable.Items.IndexOf(name);
                if (idx >= 0)
                {
                    lbAvailable.SelectedIndex = idx;
                    lbAvailable.Focus();
                }

                txtNewName.Text = string.Empty;
                txtNewOSC.Text = string.Empty;
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in btnAddNew_Click: " + ex.Message);
            }
        }

        private void lbAvailable_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (lbAvailable.SelectedItem == null)
                {
                    txtNewName.Text = string.Empty;
                    txtNewOSC.Text = string.Empty;
                    return;
                }

                var name = lbAvailable.SelectedItem.ToString();
                var mapping = mainForm.setlistMappings.FirstOrDefault(a => a.Element("name").Value == name);
                if (mapping != null)
                {
                    txtNewName.Text = mapping.Element("name").Value;
                    var osc = mapping.Element("oscaddress");
                    txtNewOSC.Text = osc != null ? osc.Value : string.Empty;
                }
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in lbAvailable_SelectedIndexChanged: " + ex.Message);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (lbAvailable.SelectedItem == null)
                    return;

                var originalName = lbAvailable.SelectedItem.ToString();
                var mapping = mainForm.setlistMappings.FirstOrDefault(a => a.Element("name").Value == originalName);
                if (mapping == null)
                    return;

                var newName = txtNewName.Text.Trim();
                var newOsc = txtNewOSC.Text.Trim();

                if (string.IsNullOrEmpty(newName))
                {
                    MessageBox.Show(this, "Please enter a name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // prevent duplicate names (except for the current item)
                if (mainForm.setlistMappings.Any(s => string.Equals(s.Element("name").Value, newName, StringComparison.OrdinalIgnoreCase) && s != mapping))
                {
                    MessageBox.Show(this, "An item with that name already exists.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                mapping.Element("name").Value = newName;

                var oscEl = mapping.Element("oscaddress");
                if (oscEl == null)
                    mapping.Add(new XElement("oscaddress", newOsc));
                else
                    oscEl.Value = newOsc;

                FillLBAvailable();
                FillLBAllocated();

                var idx = lbAvailable.Items.IndexOf(newName);
                if (idx >= 0)
                    lbAvailable.SelectedIndex = idx;
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in btnUpdate_Click: " + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            try
            {
                // prefer deleting from available list if an item is selected there
                string name = null;
                if (lbAvailable.SelectedItem != null)
                    name = lbAvailable.SelectedItem.ToString();
                else if (clbAllocated.SelectedItem != null)
                {
                    var item = clbAllocated.SelectedItem.ToString();
                    name = item.Contains(". ") ? item.Substring(item.IndexOf('.') + 2) : item;
                }

                if (string.IsNullOrEmpty(name))
                    return;

                var confirm = MessageBox.Show(this, "Delete '" + name + "'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes)
                    return;

                var mapping = mainForm.setlistMappings.FirstOrDefault(a => string.Equals(a.Element("name").Value, name, StringComparison.OrdinalIgnoreCase));
                if (mapping != null)
                {
                    mainForm.setlistMappings.Remove(mapping);
                }

                // refresh lists and mappings
                FillLBAvailable();
                FillLBAllocated();
                UpdateMappings();

                txtNewName.Text = string.Empty;
                txtNewOSC.Text = string.Empty;
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in btnDelete_Click: " + ex.Message);
            }
        }

        // Drag & drop handlers to allow moving items between lists and reordering
        private void lbAvailable_MouseDown(object sender, MouseEventArgs e)
        {
            int idx = lbAvailable.IndexFromPoint(e.Location);
            if (idx < 0) return;

            // ensure the clicked item becomes selected and populate fields immediately
            lbAvailable.SelectedIndex = idx;
            try
            {
                // call the same logic as the SelectedIndexChanged handler to populate text boxes
                lbAvailable_SelectedIndexChanged(lbAvailable, EventArgs.Empty);
            }
            catch { }

            var name = lbAvailable.Items[idx].ToString();
            lbAvailable.DoDragDrop("LB:" + name, DragDropEffects.Move);
        }

        private void lbAvailable_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void lbAvailable_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var data = e.Data.GetData(DataFormats.Text) as string;
                if (string.IsNullOrEmpty(data)) return;

                if (data.StartsWith("CLB:"))
                {
                    int srcIndex = int.Parse(data.Substring(4));
                    if (srcIndex >= 0 && srcIndex < clbAllocated.Items.Count)
                    {
                        clbAllocated.Items.RemoveAt(srcIndex);
                        RebuildAllocatedDisplay();
                        UpdateMappings();
                        FillLBAvailable();
                        lbAvailable.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in lbAvailable_DragDrop: " + ex.Message);
            }
        }

        private void clbAllocated_MouseDown(object sender, MouseEventArgs e)
        {
            int idx = clbAllocated.IndexFromPoint(e.Location);
            if (idx < 0) return;

            clbAllocated.DoDragDrop("CLB:" + idx.ToString(), DragDropEffects.Move);
        }

        private void clbAllocated_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        private void clbAllocated_DragOver(object sender, DragEventArgs e)
        {
            // keep effect so cursor indicates move
            e.Effect = DragDropEffects.Move;
        }

        private void clbAllocated_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var data = e.Data.GetData(DataFormats.Text) as string;
                if (string.IsNullOrEmpty(data)) return;

                // determine drop target index
                var clientPoint = clbAllocated.PointToClient(new Point(e.X, e.Y));
                int targetIndex = clbAllocated.IndexFromPoint(clientPoint);
                if (targetIndex < 0) targetIndex = clbAllocated.Items.Count;

                if (data.StartsWith("LB:"))
                {
                    var name = data.Substring(3);
                    // insert the name (without numbering) and preserve unchecked state
                    clbAllocated.Items.Insert(targetIndex, name);
                    clbAllocated.SetItemChecked(targetIndex, false);
                    RebuildAllocatedDisplay();
                    UpdateMappings();
                    FillLBAvailable();
                    lbAvailable.Focus();
                    clbAllocated.SelectedIndex = targetIndex;
                }
                else if (data.StartsWith("CLB:"))
                {
                    int srcIndex = int.Parse(data.Substring(4));
                    if (srcIndex < 0 || srcIndex >= clbAllocated.Items.Count) return;

                    if (srcIndex == targetIndex || srcIndex == targetIndex - 1)
                        return; // no-op

                    bool wasChecked = clbAllocated.GetItemChecked(srcIndex);
                    var item = clbAllocated.Items[srcIndex];
                    clbAllocated.Items.RemoveAt(srcIndex);
                    if (srcIndex < targetIndex) targetIndex--;
                    clbAllocated.Items.Insert(targetIndex, item);
                    clbAllocated.SetItemChecked(targetIndex, wasChecked);
                    RebuildAllocatedDisplay();
                    UpdateMappings();
                    clbAllocated.SelectedIndex = targetIndex;
                }
            }
            catch (Exception ex)
            {
                mainForm.OutputText("Error in clbAllocated_DragDrop: " + ex.Message);
            }
        }

        private void RebuildAllocatedDisplay()
        {
            // capture current names and checked states
            var names = new List<string>();
            var checks = new List<bool>();

            for (int i = 0; i < clbAllocated.Items.Count; i++)
            {
                var s = clbAllocated.Items[i].ToString();
                var name = s;
                if (s.Contains(". "))
                    name = s.Substring(s.IndexOf('.') + 2);
                names.Add(name);
                checks.Add(clbAllocated.GetItemChecked(i));
            }

            clbAllocated.Items.Clear();

            for (int i = 0; i < names.Count; i++)
            {
                clbAllocated.Items.Add((i + 1).ToString() + ". " + names[i], checks[i]);
            }
        }

    }
}
