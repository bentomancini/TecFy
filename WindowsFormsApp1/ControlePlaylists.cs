using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class ControlePlaylists : UserControl
    {
        private readonly int _usuarioId;
        private ListBox lstPlaylists;
        private FlowLayoutPanel flpMusicas;
        private Label lblStatus;
        private Point _pontoInicioArrasto;
        private bool _arrastando;

        // Dispara quando o usuario pede para tocar uma musica da playlist.
        public event Action<SpotifyService.Faixa> FaixaSolicitada;

        // Dispara quando a lista de playlists muda (criou, renomeou ou apagou).
        public event Action PlaylistsAlteradas;

        public ControlePlaylists(int usuarioId)
        {
            _usuarioId = usuarioId;
            Inicializar();
        }

        private void Inicializar()
        {
            BackColor = Color.FromArgb(13, 7, 20);
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(18);

            var lblTitulo = new Label
            {
                Text = "Suas playlists",
                ForeColor = Color.FromArgb(168, 85, 247),
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 12)
            };

            // Painel de baixo: barbotões + lista de musicas.
            var pnlInferior = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 0)
            };

            var flpBotoes = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 34,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            var btnVer = CriarBotao("Ver musicas", Color.FromArgb(124, 58, 237));
            btnVer.Click += BtnVer_Click;

            var btnNova = CriarBotao("+ Nova", Color.FromArgb(45, 20, 65));
            btnNova.Click += BtnNova_Click;

            var btnRenomear = CriarBotao("Renomear", Color.FromArgb(45, 20, 65));
            btnRenomear.Click += BtnRenomear_Click;

            var btnApagar = CriarBotao("Apagar", Color.FromArgb(200, 40, 70));
            btnApagar.Click += BtnApagar_Click;

            flpBotoes.Controls.Add(btnVer);
            flpBotoes.Controls.Add(btnNova);
            flpBotoes.Controls.Add(btnRenomear);
            flpBotoes.Controls.Add(btnApagar);

            lblStatus = new Label
            {
                Text = "",
                ForeColor = Color.HotPink,
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 4)
            };

            var lblFaixas = new Label
            {
                Text = "Musicas da playlist selecionada:",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F),
                AutoSize = true,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 0, 0, 8)
            };

            flpMusicas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                AllowDrop = true,
                BackColor = Color.FromArgb(20, 11, 30)
            };
            flpMusicas.DragEnter += flpMusicas_DragEnter;
            flpMusicas.DragDrop += flpMusicas_DragDrop;

            pnlInferior.Controls.Add(flpMusicas);
            pnlInferior.Controls.Add(lblFaixas);
            pnlInferior.Controls.Add(lblStatus);
            pnlInferior.Controls.Add(flpBotoes);

            // Painel da esquerda (lista de playlists) e lado direito (conteudo).
            var lstPlaylistsPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 200,
                Padding = new Padding(0, 4, 10, 0)
            };

            lstPlaylists = new ListBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(28, 16, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 12.5F)
            };
            lstPlaylists.SelectedIndexChanged += (s, e) => CarregarMusicas();
            lstPlaylistsPanel.Controls.Add(lstPlaylists);

            var pnlDireito = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 4, 0, 0)
            };
            pnlDireito.Controls.Add(pnlInferior);

            Controls.Add(pnlDireito);
            Controls.Add(lstPlaylistsPanel);
            Controls.Add(lblTitulo);

            CarregarPlaylists();
            CarregarMusicas();
        }

        private Button CriarBotao(string texto, Color cor)
        {
            var botao = new Button
            {
                Text = texto,
                AutoSize = false,
                Size = new Size(96, 30),
                Margin = new Padding(0, 0, 8, 0),
                BackColor = cor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            botao.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(botao, 15);
            return botao;
        }

        public void Atualizar()
        {
            CarregarPlaylists();
        }

        // Seleciona e carrega a playlist indicada, caso exista na lista.
        public bool AbrirPlaylist(int idPlaylist)
        {
            foreach (object obj in lstPlaylists.Items)
            {
                if (obj is ItemPlaylist item && item.Id == idPlaylist)
                {
                    lstPlaylists.SelectedItem = item;
                    lstPlaylists.TopIndex = lstPlaylists.Items.IndexOf(item);
                    CarregarMusicas();
                    return true;
                }
            }
            return false;
        }

        private void CarregarPlaylists()
        {
            lstPlaylists.Items.Clear();

            try
            {
                DataTable tabela = PlaylistDAO.ListarPorUsuario(_usuarioId);

                if (tabela.Rows.Count == 0)
                {
                    lstPlaylists.Items.Add("Voce ainda nao tem playlists.");
                    return;
                }

                foreach (DataRow linha in tabela.Rows)
                {
                    int id = Convert.ToInt32(linha["Id"]);
                    string nome = Convert.ToString(linha["Nome"]);
                    lstPlaylists.Items.Add(new ItemPlaylist(id, nome));
                }
            }
            catch (Exception ex)
            {
                lstPlaylists.Items.Add("Erro ao carregar playlists: " + ex.Message);
            }

            Tema.Aplicar(this);
        }

        private void CarregarMusicas()
        {
            flpMusicas.Controls.Clear();

            if (!(lstPlaylists.SelectedItem is ItemPlaylist item))
            {
                flpMusicas.Controls.Add(Aviso("Selecione uma playlist para ver as musicas."));
                Tema.Aplicar(this);
                return;
            }

            try
            {
                List<SpotifyService.Faixa> faixas = PlaylistDAO.ListarMusicas(item.Id);

                if (faixas == null || faixas.Count == 0)
                {
                    flpMusicas.Controls.Add(Aviso("Esta playlist ainda nao tem musicas."));
                    Tema.Aplicar(this);
                    return;
                }

                foreach (var faixa in faixas)
                {
                    flpMusicas.Controls.Add(CriarItem(faixa, item.Id));
                }
            }
            catch (Exception ex)
            {
                flpMusicas.Controls.Add(Aviso("Erro ao carregar musicas: " + ex.Message));
            }

            Tema.Aplicar(this);
        }

        private void Linha_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pontoInicioArrasto = e.Location;
                _arrastando = false;
            }
        }

        private void Linha_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _arrastando || sender == null)
                return;

            if (Math.Abs(e.X - _pontoInicioArrasto.X) > SystemInformation.DragSize.Width
                || Math.Abs(e.Y - _pontoInicioArrasto.Y) > SystemInformation.DragSize.Height)
            {
                _arrastando = true;
                var origem = sender as Panel;
                if (origem != null)
                    origem.DoDragDrop(origem, DragDropEffects.Move);
                _arrastando = false;
            }
        }

        private void flpMusicas_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(Panel)) && lstPlaylists.SelectedItem is ItemPlaylist
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }

        private void flpMusicas_DragDrop(object sender, DragEventArgs e)
        {
            var origem = e.Data.GetData(typeof(Panel)) as Panel;
            if (origem == null || !(lstPlaylists.SelectedItem is ItemPlaylist item))
                return;

            var itens = new List<Control>();
            foreach (Control c in flpMusicas.Controls)
                itens.Add(c);

            int indiceOrigem = itens.IndexOf(origem);
            if (indiceOrigem < 0)
                return;

            Point pt = flpMusicas.PointToClient(new Point(e.X, e.Y));
            int indiceAlvo = itens.Count - 1;
            for (int i = 0; i < itens.Count; i++)
            {
                if (itens[i] != origem && itens[i].Bounds.Contains(pt))
                {
                    indiceAlvo = i;
                    break;
                }
            }

            if (indiceAlvo > indiceOrigem)
                indiceAlvo--;

            var movido = itens[indiceOrigem];
            itens.RemoveAt(indiceOrigem);
            itens.Insert(indiceAlvo, movido);

            flpMusicas.SuspendLayout();
            flpMusicas.Controls.Clear();
            foreach (var c in itens)
                flpMusicas.Controls.Add(c);
            flpMusicas.ResumeLayout(true);

            var ids = new List<int>();
            foreach (var c in itens)
                if (c is Panel p && p.Tag is SpotifyService.Faixa f && f.MusicaId.HasValue)
                    ids.Add(f.MusicaId.Value);

            if (ids.Count > 1)
            {
                var resultado = PlaylistDAO.Reordenar(item.Id, ids);
                lblStatus.Text = resultado.Ok
                    ? "Ordem da playlist atualizada."
                    : resultado.Erro;
            }
            else
            {
                lblStatus.Text = "";
            }

            Tema.Aplicar(flpMusicas);
        }

        private Control Aviso(string texto)
        {
            return new Label
            {
                Text = texto,
                ForeColor = Color.FromArgb(168, 85, 247),
                AutoSize = true,
                Padding = new Padding(5),
                Margin = new Padding(2)
            };
        }

        private Control CriarItem(SpotifyService.Faixa faixa, int idPlaylist)
        {
            var linha = new Panel
            {
                Size = new Size(440, 70),
                Margin = new Padding(2),
                Tag = faixa
            };
            linha.MouseDown += Linha_MouseDown;
            linha.MouseMove += Linha_MouseMove;

            var picCapa = new PictureBox
            {
                Size = new Size(58, 58),
                Location = new Point(2, 6),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(45, 20, 65),
                Tag = faixa
            };
            Tema.Arredondar(picCapa, 10);
            if (!string.IsNullOrWhiteSpace(faixa.ImagemUrl))
            {
                try { picCapa.LoadAsync(faixa.ImagemUrl); } catch { }
            }

            var btnPlay = new Button
            {
                Text = "▶",
                Size = new Size(42, 42),
                Location = new Point(68, 14),
                BackColor = Color.FromArgb(124, 58, 237),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnPlay.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnPlay, 21);
            btnPlay.Tag = faixa;
            btnPlay.Click += BtnTocar_Click;

            var lblNome = new Label
            {
                Text = faixa.Nome + "  -  " + faixa.Artistas,
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(120, 22),
                Font = new Font("Segoe UI", 13F),
                MaximumSize = new Size(220, 26)
            };

            var lblDur = new Label
            {
                Text = faixa.DuracaoSegundos > 0
                    ? TimeSpan.FromSeconds(faixa.DuracaoSegundos).ToString(@"mm\:ss")
                    : "",
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(346, 25),
                Font = new Font("Segoe UI", 11F)
            };

            var btnRemover = new Button
            {
                Text = "✕",
                Size = new Size(42, 42),
                Location = new Point(390, 14),
                BackColor = Color.FromArgb(45, 20, 65),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnRemover.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnRemover, 21);
            btnRemover.Tag = faixa;
            btnRemover.Click += (s, e) => BtnRemover_Click(faixa, idPlaylist);

            linha.Controls.Add(picCapa);
            linha.Controls.Add(btnPlay);
            linha.Controls.Add(lblNome);
            linha.Controls.Add(lblDur);
            linha.Controls.Add(btnRemover);
            return linha;
        }

        private void BtnRemover_Click(SpotifyService.Faixa faixa, int idPlaylist)
        {
            if (!(lstPlaylists.SelectedItem is ItemPlaylist item))
                return;

            if (MessageBox.Show(ParentForm,
                "Remover \"" + faixa.Nome + "\" desta playlist?",
                "Remover musica", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var resultado = PlaylistDAO.RemoverMusica(idPlaylist, faixa.MusicaId ?? 0);
            if (resultado.Ok)
            {
                lblStatus.Text = "Removida da playlist: " + faixa.Nome;
                CarregarMusicas();
            }
            else
            {
                lblStatus.Text = resultado.Erro;
            }
        }

        private void BtnTocar_Click(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.Tag is SpotifyService.Faixa faixa)
            {
                FaixaSolicitada?.Invoke(faixa);
                lblStatus.Text = "Tocando: " + faixa.Nome;
            }
        }

        private void BtnVer_Click(object sender, EventArgs e)
        {
            if (!(lstPlaylists.SelectedItem is ItemPlaylist))
            {
                lblStatus.Text = "Selecione uma playlist primeiro.";
                return;
            }
            CarregarMusicas();
            lblStatus.Text = "";
        }

        private void BtnNova_Click(object sender, EventArgs e)
        {
            using (var form = new FormNovaPlaylist(_usuarioId))
            {
                form.ShowDialog(ParentForm);
            }
            CarregarPlaylists();
            PlaylistsAlteradas?.Invoke();
        }

        private void BtnRenomear_Click(object sender, EventArgs e)
        {
            if (!(lstPlaylists.SelectedItem is ItemPlaylist item))
            {
                lblStatus.Text = "Selecione uma playlist para renomear.";
                return;
            }

            using (var form = new FormRenomearPlaylist(item.Id, item.Nome))
            {
                if (form.ShowDialog(ParentForm) == DialogResult.OK)
                {
                    CarregarPlaylists();
                    PlaylistsAlteradas?.Invoke();
                }
            }
        }

        private void BtnApagar_Click(object sender, EventArgs e)
        {
            if (!(lstPlaylists.SelectedItem is ItemPlaylist item))
            {
                lblStatus.Text = "Selecione uma playlist para apagar.";
                return;
            }

            if (MessageBox.Show(ParentForm,
                "Apagar a playlist \"" + item.Nome + "\"? Esta acao nao pode ser desfeita.",
                "Apagar playlist", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var resultado = PlaylistDAO.Excluir(item.Id);
            if (resultado.Ok)
            {
                lblStatus.Text = "Playlist apagada: " + item.Nome;
                lstPlaylists.Items.Remove(item);
                flpMusicas.Controls.Clear();
                PlaylistsAlteradas?.Invoke();
            }
            else
            {
                lblStatus.Text = resultado.Erro;
            }
        }

        private class ItemPlaylist
        {
            public int Id;
            public string Nome;

            public ItemPlaylist(int id, string nome)
            {
                Id = id;
                Nome = nome;
            }

            public override string ToString() => Nome;
        }
    }
}
