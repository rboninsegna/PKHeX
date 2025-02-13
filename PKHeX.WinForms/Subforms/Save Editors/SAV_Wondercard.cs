﻿using PKHeX.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using static PKHeX.Core.MessageStrings;

namespace PKHeX.WinForms
{
    public partial class SAV_Wondercard : Form
    {
        private readonly SaveFile Origin;
        private readonly SaveFile SAV;

        public SAV_Wondercard(SaveFile sav, MysteryGift g = null)
        {
            InitializeComponent();
            WinFormsUtil.TranslateInterface(this, Main.CurrentLanguage);
            SAV = (Origin = sav).Clone();
            mga = SAV.GiftAlbum;

            switch (SAV.Generation)
            {
                case 4:
                    pba = PopulateViewGiftsG4().ToArray();
                    break;
                case 5:
                case 6:
                case 7:
                    pba = PopulateViewGiftsG567().ToArray();
                    break;
                default:
                    throw new ArgumentException("Game not supported.");
            }

            foreach (PictureBox pb in pba)
            {
                pb.AllowDrop = true;
                pb.DragDrop += BoxSlot_DragDrop;
                pb.DragEnter += BoxSlot_DragEnter;
                pb.MouseDown += BoxSlot_MouseDown;
                pb.ContextMenuStrip = mnuVSD;
            }

            SetGiftBoxes();
            GetReceivedFlags();

            if (LB_Received.Items.Count > 0)
                LB_Received.SelectedIndex = 0;

            DragEnter += Main_DragEnter;
            DragDrop += Main_DragDrop;

            if (g == null)
                ClickView(pba[0], null);
            else
                ViewGiftData(g);
        }

        private readonly MysteryGiftAlbum mga;
        private MysteryGift mg;
        private readonly PictureBox[] pba;

        // Repopulation Functions
        private void SetBackground(int index, Image bg)
        {
            for (int i = 0; i < mga.Gifts.Length; i++)
                pba[i].BackgroundImage = index == i ? bg : null;
        }

        private void SetGiftBoxes()
        {
            for (int i = 0; i < mga.Gifts.Length; i++)
            {
                MysteryGift m = mga.Gifts[i];
                pba[i].Image = m.Sprite();
            }
        }

        private void ViewGiftData(MysteryGift g)
        {
            try
            {
                // only check if the form is visible (not opening)
                if (Visible && g.GiftUsed && DialogResult.Yes == WinFormsUtil.Prompt(MessageBoxButtons.YesNo, MsgMsyteryGiftUsedAlert, MsgMysteryGiftUsedFix))
                    g.GiftUsed = false;

                RTB.Lines = g.GetDescription().ToArray();
                PB_Preview.Image = g.Sprite();
                mg = g;
            }
            catch (Exception e)
            {
                WinFormsUtil.Error(MsgMysteryGiftParseTypeUnknown, e);
                RTB.Clear();
            }
        }

        private void GetReceivedFlags()
        {
            LB_Received.Items.Clear();
            for (int i = 1; i < mga.Flags.Length; i++)
            {
                if (mga.Flags[i])
                    LB_Received.Items.Add(i.ToString("0000"));
            }

            if (LB_Received.Items.Count > 0)
                LB_Received.SelectedIndex = 0;
        }

        private void SetCardID(int cardID)
        {
            if (cardID <= 0 || cardID >= 0x100 * 8) return;

            string card = cardID.ToString("0000");
            if (!LB_Received.Items.Contains(card))
                LB_Received.Items.Add(card);
            LB_Received.SelectedIndex = LB_Received.Items.IndexOf(card);
        }

        // Mystery Gift IO (.file<->window)
        private void B_Import_Click(object sender, EventArgs e)
        {
            OpenFileDialog import = new OpenFileDialog {Filter = WinFormsUtil.GetMysterGiftFilter(SAV.Generation, SAV.Version) };
            if (import.ShowDialog() != DialogResult.OK) return;

            string path = import.FileName;
            MysteryGift g = MysteryGift.GetMysteryGift(File.ReadAllBytes(path), Path.GetExtension(path));
            if (g == null)
            {
                WinFormsUtil.Error(MsgMysteryGiftInvalid, path);
                return;
            }
            ViewGiftData(g);
        }

        private void B_Output_Click(object sender, EventArgs e)
        {
            WinFormsUtil.ExportMGDialog(mg, SAV.Version);
        }

        private static int GetLastUnfilledByType(MysteryGift Gift, MysteryGiftAlbum Album)
        {
            for (int i = 0; i < Album.Gifts.Length; i++)
            {
                if (!Album.Gifts[i].Empty)
                    continue;
                if (Album.Gifts[i].Type != Gift.Type)
                    continue;
                return i;
            }
            return -1;
        }
        // Mystery Gift RW (window<->sav)
        private void ClickView(object sender, EventArgs e)
        {
            sender = WinFormsUtil.GetUnderlyingControl(sender);
            int index = Array.IndexOf(pba, sender);

            SetBackground(index, Properties.Resources.slotView);
            ViewGiftData(mga.Gifts[index]);
        }

        private void ClickSet(object sender, EventArgs e)
        {
            if (!mg.IsCardCompatible(SAV, out var msg))
            {
                WinFormsUtil.Alert(MsgMysteryGiftSlotFail, msg);
                return;
            }

            sender = WinFormsUtil.GetUnderlyingControl(sender);
            int index = Array.IndexOf(pba, sender);

            // Hijack to the latest unfilled slot if index creates interstitial empty slots.
            int lastUnfilled = GetLastUnfilledByType(mg, mga);
            if (lastUnfilled > -1 && lastUnfilled < index)
                index = lastUnfilled;

            if (mg is PCD pcd && mga.Gifts[index] is PGT)
            {
                mg = pcd.Gift;
            }
            else if (mg.Type != mga.Gifts[index].Type)
            {
                WinFormsUtil.Alert(MsgMysteryGiftSlotFail, $"{mg.Type} != {mga.Gifts[index].Type}");
                return;
            }
            SetBackground(index, Properties.Resources.slotSet);
            mga.Gifts[index] = mg.Clone();
            SetGiftBoxes();
            SetCardID(mg.CardID);
        }

        private void ClickDelete(object sender, EventArgs e)
        {
            sender = WinFormsUtil.GetUnderlyingControl(sender);
            int index = Array.IndexOf(pba, sender);

            mga.Gifts[index].Data = new byte[mga.Gifts[index].Data.Length];

            // Shuffle blank card down
            int i = index;
            while (i < mga.Gifts.Length - 1)
            {
                if (mga.Gifts[i+1].Empty)
                    break;
                if (mga.Gifts[i+1].Type != mga.Gifts[i].Type)
                    break;

                i++;

                var mg1 = mga.Gifts[i];
                var mg2 = mga.Gifts[i-1];

                mga.Gifts[i-1] = mg1;
                mga.Gifts[i] = mg2;
            }
            SetBackground(i, Properties.Resources.slotDel);
            SetGiftBoxes();
        }

        // Close Window
        private void B_Cancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void B_Save_Click(object sender, EventArgs e)
        {
            // Make sure all of the Received Flags are flipped!
            bool[] flags = new bool[mga.Flags.Length];
            foreach (var o in LB_Received.Items)
                flags[Util.ToUInt32(o.ToString())] = true;

            mga.Flags = flags;
            SAV.GiftAlbum = mga;

            Origin.SetData(SAV.Data, 0);
            Close();
        }

        // Delete Received Flag
        private void ClearRecievedFlag(object sender, EventArgs e)
        {
            if (LB_Received.SelectedIndex < 0) return;

            if (LB_Received.Items.Count > 0)
                LB_Received.Items.Remove(LB_Received.Items[LB_Received.SelectedIndex]);
            if (LB_Received.Items.Count > 0)
                LB_Received.SelectedIndex = 0;
        }

        // Drag & Drop Wonder Cards
        private static void Main_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Main_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Check for multiple wondercards
            if (Directory.Exists(files[0]))
                files = Directory.GetFiles(files[0], "*", SearchOption.AllDirectories);
            if (files.Length == 1 && !Directory.Exists(files[0]))
            {
                string path = files[0]; // open first D&D
                if (!MysteryGift.IsMysteryGift(new FileInfo(path).Length)) // arbitrary
                {
                    WinFormsUtil.Alert(MsgMysteryGiftInvalid, path);
                    return;
                }
                var gift = MysteryGift.GetMysteryGift(File.ReadAllBytes(path), Path.GetExtension(path));
                if (gift == null)
                {
                    WinFormsUtil.Error(MsgMysteryGiftInvalid, path);
                    return;
                }
                ViewGiftData(gift);
                return;
            }
            SetGiftBoxes();
        }

        private void ClickQR(object sender, EventArgs e)
        {
            if (ModifierKeys == Keys.Alt)
            {
                string url = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    ImportQRToView(url);
                    return;
                }
            }
            ExportQRFromView();
        }

        private void ExportQRFromView()
        {
            if (mg.Data.All(z => z == 0))
            {
                WinFormsUtil.Alert(MsgMysteryGiftSlotNone);
                return;
            }
            if (SAV.Generation == 6 && mg.ItemID == 726 && mg.IsItem)
            {
                WinFormsUtil.Alert(MsgMysteryGiftQREonTicket, MsgMysteryGiftQREonTicketAdvice);
                return;
            }

            var qrmessage = QRMessageUtil.GetMessage(mg);
            Image qr = QR.GetQRImage(qrmessage);
            if (qr == null)
                return;

            string desc = $"({mg.Type}) {string.Join(Environment.NewLine, mg.GetDescription())}";

            new QR(qr, PB_Preview.Image, null, desc + Environment.NewLine + "PKHeX Wonder Card @ ProjectPokemon.org").ShowDialog();
        }

        private void ImportQRToView(string url)
        {
            byte[] data = QR.GetQRData(url);
            if (data.Length == 0)
                return;

            string[] types = mga.Gifts.Select(g => g.Type).Distinct().ToArray();
            MysteryGift gift = MysteryGift.GetMysteryGift(data);
            string giftType = gift.Type;

            if (mga.Gifts.All(card => card.Data.Length != data.Length))
                WinFormsUtil.Alert(MsgMysteryGiftQRTypeLength, string.Format(MsgQRDecodeSize, $"0x{data.Length:X}"));
            else if (types.All(type => type != giftType))
                WinFormsUtil.Alert(MsgMysteryGiftTypeIncompatible, $"{MsgMysteryGiftQRRecieved} {gift.Type}{Environment.NewLine}{MsgMysteryGiftTypeUnexpected} {string.Join(", ", types)}");
            else if (!SAV.CanRecieveGift(gift))
                WinFormsUtil.Alert(MsgMysteryGiftTypeDetails);
            else
                ViewGiftData(gift);
        }

        private void BoxSlot_MouseDown(object sender, MouseEventArgs e)
        {
            switch (ModifierKeys)
            {
                case Keys.Control: ClickView(sender, e); return;
                case Keys.Shift: ClickSet(sender, e); return;
                case Keys.Alt: ClickDelete(sender, e); return;
            }
            var pb = sender as PictureBox;
            if (pb?.Image == null)
                return;

            if (e.Button != MouseButtons.Left || e.Clicks != 1) return;

            int index = Array.IndexOf(pba, sender);
            wc_slot = index;
            // Create Temp File to Drag
            Cursor.Current = Cursors.Hand;

            // Make File
            var gift = mga.Gifts[index];
            string newfile = Path.Combine(Path.GetTempPath(), Util.CleanFileName(gift.FileName));
            try
            {
                File.WriteAllBytes(newfile, gift.Data);
                DoDragDrop(new DataObject(DataFormats.FileDrop, new[] { newfile }), DragDropEffects.Move);
            }
            catch (Exception x)
            { WinFormsUtil.Error("Drag & Drop Error", x); }
            File.Delete(newfile);
            wc_slot = -1;
        }

        private void BoxSlot_DragDrop(object sender, DragEventArgs e)
        {
            int index = Array.IndexOf(pba, sender);

            // Hijack to the latest unfilled slot if index creates interstitial empty slots.
            int lastUnfilled = GetLastUnfilledByType(mg, mga);
            if (lastUnfilled > -1 && lastUnfilled < index && mga.Gifts[lastUnfilled].Type == mga.Gifts[index].Type)
                index = lastUnfilled;

            if (wc_slot == -1) // dropped
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length < 1)
                    return;
                if (!MysteryGift.IsMysteryGift(new FileInfo(files[0]).Length))
                { WinFormsUtil.Alert(MsgFileUnsupported, files[0]); return; }

                byte[] data = File.ReadAllBytes(files[0]);
                MysteryGift gift = MysteryGift.GetMysteryGift(data, new FileInfo(files[0]).Extension);

                if (gift is PCD pcd && mga.Gifts[index] is PGT)
                {
                    gift = pcd.Gift;
                }
                else if (gift.Type != mga.Gifts[index].Type)
                {
                    WinFormsUtil.Alert(MsgMysteryGiftSlotFail, $"{gift.Type} != {mga.Gifts[index].Type}");
                    return;
                }
                SetBackground(index, Properties.Resources.slotSet);
                mga.Gifts[index] = gift.Clone();

                SetCardID(mga.Gifts[index].CardID);
                ViewGiftData(mga.Gifts[index]);
            }
            else // Swap Data
            {
                MysteryGift s1 = mga.Gifts[index];
                MysteryGift s2 = mga.Gifts[wc_slot];

                if (s2 is PCD && s1 is PGT)
                {
                    // set the PGT to the PGT slot instead
                    ViewGiftData(s2);
                    ClickSet(pba[index], null);
                    { WinFormsUtil.Alert(string.Format(MsgMysteryGiftSlotAlternate, s2.Type, s1.Type)); return; }
                }
                if (s1.Type != s2.Type)
                { WinFormsUtil.Alert(string.Format(MsgMysteryGiftSlotFailSwap, s2.Type, s1.Type)); return; }
                mga.Gifts[wc_slot] = s1;
                mga.Gifts[index] = s2;

                if (mga.Gifts[wc_slot].Empty) // empty slot created, slide down
                {
                    int i = wc_slot;
                    while (i < index)
                    {
                        if (mga.Gifts[i + 1].Empty)
                            break;
                        if (mga.Gifts[i + 1].Type != mga.Gifts[i].Type)
                            break;

                        i++;

                        var mg1 = mga.Gifts[i];
                        var mg2 = mga.Gifts[i - 1];

                        mga.Gifts[i - 1] = mg1;
                        mga.Gifts[i] = mg2;
                    }
                    index = i-1;
                }
            }
            SetBackground(index, Properties.Resources.slotView);
            SetGiftBoxes();
        }

        private static void BoxSlot_DragEnter(object sender, DragEventArgs e)
        {
            if (e.AllowedEffect == (DragDropEffects.Copy | DragDropEffects.Link)) // external file
                e.Effect = DragDropEffects.Copy;
            else if (e.Data != null) // within
                e.Effect = DragDropEffects.Move;
        }

        private int wc_slot = -1;

        // UI Generation
        private List<PictureBox> PopulateViewGiftsG4()
        {
            List<PictureBox> pb = new List<PictureBox>();

            // Row 1
            var f1 = GetFlowLayoutPanel();
            f1.Controls.Add(GetLabel($"{nameof(PGT)} 1-6"));
            for (int i = 0; i < 6; i++)
            {
                var p = GetPictureBox();
                f1.Controls.Add(p);
                pb.Add(p);
            }
            // Row 2
            var f2 = GetFlowLayoutPanel();
            f2.Controls.Add(GetLabel($"{nameof(PGT)} 7-8"));
            for (int i = 6; i < 8; i++)
            {
                var p = GetPictureBox();
                f2.Controls.Add(p);
                pb.Add(p);
            }
            // Row 3
            var f3 = GetFlowLayoutPanel();
            f3.Margin = new Padding(0, f3.Height, 0, 0);
            f3.Controls.Add(GetLabel($"{nameof(PCD)} 1-3"));
            for (int i = 8; i < 11; i++)
            {
                var p = GetPictureBox();
                f3.Controls.Add(p);
                pb.Add(p);
            }

            FLP_Gifts.Controls.Add(f1);
            FLP_Gifts.Controls.Add(f2);
            FLP_Gifts.Controls.Add(f3);
            return pb;
        }

        private List<PictureBox> PopulateViewGiftsG567()
        {
            var pb = new List<PictureBox>();

            const int cellsPerRow = 6;
            int rows = (int)Math.Ceiling(mga.Gifts.Length / (decimal)cellsPerRow);
            int countRemaining = mga.Gifts.Length;

            for (int i = 0; i < rows; i++)
            {
                var row = GetFlowLayoutPanel();
                int count = cellsPerRow >= countRemaining ? countRemaining : cellsPerRow;
                countRemaining -= count;
                int start = (i * cellsPerRow) + 1;
                row.Controls.Add(GetLabel($"{start}-{start + count - 1}"));
                for (int j = 0; j < count; j++)
                {
                    var p = GetPictureBox();
                    row.Controls.Add(p);
                    pb.Add(p);
                }
                FLP_Gifts.Controls.Add(row);
            }
            return pb;
        }

        private static FlowLayoutPanel GetFlowLayoutPanel()
        {
            return new FlowLayoutPanel
            {
                Width = 305,
                Height = 34,
                Padding = new Padding(0),
                Margin = new Padding(0),
            };
        }

        private static Label GetLabel(string text)
        {
            return new Label
            {
                Size = new Size(40, 34),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
                Text = text,
                Padding = new Padding(0),
                Margin = new Padding(0),
            };
        }

        private static PictureBox GetPictureBox()
        {
            return new PictureBox
            {
                Size = new Size(42, 32),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.Transparent,
                Padding = new Padding(0),
                Margin = new Padding(1),
            };
        }

        private void B_ModifyAll_Click(object sender, EventArgs e)
        {
            foreach (var g in mga.Gifts)
                g.GiftUsed = sender == B_UsedAll;
            SetGiftBoxes();
            System.Media.SystemSounds.Asterisk.Play();
        }
    }
}