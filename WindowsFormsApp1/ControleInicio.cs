using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class ControleInicio : UserControl
    {
        private readonly int _usuarioId;
        private FlowLayoutPanel flpScroll;
        private Label lblCarregando;
        private Label lblBemVindo;
        private Panel pnlHero;
        private FlowLayoutPanel flpArtistas;
        private FlowLayoutPanel flpAlbuns;
        private bool _carregado;
        private bool _ajustandoLayout;

        private const int LarguraCard = 190;
        private const int AlturaCard = 240;
        private const int EspacoCard = 12;

        // Eventos disparados ao clicar num card de artista ou de album.
        public event Action<SpotifyService.Artista> ArtistaSolicitado;
        public event Action<string> AlbumSolicitado;
        public event Action ExplorarArtistasSolicitado;

        public ControleInicio(int usuarioId, string nomeUsuario)
        {
            _usuarioId = usuarioId;
            Inicializar(nomeUsuario);
        }

        private void Inicializar(string nomeUsuario)
        {
            BackColor = Tema.Fundo;
            Font = new Font("Segoe UI", 9F);

            pnlHero = new Panel
            {
                Height = 194,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Tema.FundoElevado
            };
            pnlHero.Paint += (s, e) =>
            {
                using (var gradiente = new LinearGradientBrush(pnlHero.ClientRectangle,
                    Tema.Claro ? Color.FromArgb(235, 218, 255) : Color.FromArgb(70, 31, 105),
                    Tema.Claro ? Color.FromArgb(251, 248, 255) : Color.FromArgb(28, 16, 42),
                    LinearGradientMode.Horizontal))
                    e.Graphics.FillRectangle(gradiente, pnlHero.ClientRectangle);
            };

            lblBemVindo = new Label
            {
                Text = ObterSaudacao() + ", "
                    + (string.IsNullOrWhiteSpace(nomeUsuario) ? "Usuario" : nomeUsuario) + "!",
                ForeColor = Tema.Destaque,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(20, 18),
                Size = new Size(440, 26),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            };
            pnlHero.Controls.Add(lblBemVindo);

            pnlHero.Controls.Add(new Label
            {
                Text = "Sua música, seu momento.",
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                Location = new Point(18, 51),
                Size = new Size(440, 39),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            });
            pnlHero.Controls.Add(new Label
            {
                Text = "Descubra artistas, explore álbuns e dê o play nos seus favoritos.",
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 9.5F),
                Location = new Point(20, 99),
                Size = new Size(440, 37),
                BackColor = Color.Transparent
            });

            var btnExplorar = new Button
            {
                Text = "Explorar artistas  →",
                Location = new Point(20, 145),
                Size = new Size(158, 34),
                BackColor = Tema.Roxo,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExplorar.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnExplorar, 14);
            btnExplorar.Click += (s, e) => ExplorarArtistasSolicitado?.Invoke();
            pnlHero.Controls.Add(btnExplorar);

            // Container rolavel com as secoes de destaque.
            flpScroll = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent,
                Padding = new Padding(16, 14, 16, 16)
            };
            flpScroll.Controls.Add(pnlHero);

            // Mensagem de carregamento.
            lblCarregando = new Label
            {
                Text = "Carregando destaques...",
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 10F),
                AutoSize = true,
                Padding = new Padding(0, 20, 0, 0)
            };
            flpScroll.Controls.Add(lblCarregando);

            Controls.Add(flpScroll);

            flpScroll.Resize += (s, e) => ReposicionarSessoes();
        }

        public void Atualizar()
        {
            if (!_carregado)
            {
                _carregado = true;
                CarregarDestaquesAsync();
            }
        }

        public void AtualizarSaudacao(string nomeUsuario)
        {
            if (lblBemVindo == null)
                return;
            lblBemVindo.Text = ObterSaudacao() + ", "
                + (string.IsNullOrWhiteSpace(nomeUsuario) ? "Usuario" : nomeUsuario) + "!";
        }

        private async void CarregarDestaquesAsync()
        {
            List<SpotifyService.Artista> artistas = null;
            List<SpotifyService.Faixa> albuns = null;

            try
            {
                var t1 = SpotifyService.BuscarArtistasDestaqueAsync(8);
                var t2 = SpotifyService.BuscarAlbunsDestaqueAsync(6);
                await Task.WhenAll(t1, t2);
                artistas = t1.Result;
                albuns = t2.Result;
            }
            catch
            {
            }

            flpScroll.SuspendLayout();
            flpScroll.Controls.Clear();
            flpScroll.Controls.Add(pnlHero);

            // Sessao: Artistas em destaque.
            if (artistas != null && artistas.Count > 0)
            {
                var lblSecArtistas = CriarTituloSecao("Artistas em destaque",
                    "Escolha um artista para ver suas músicas mais populares");
                flpScroll.Controls.Add(lblSecArtistas);

                flpArtistas = CriarGradeDestaque();
                foreach (var artista in artistas)
                {
                    flpArtistas.Controls.Add(
                        CriarCardDestaque(
                            artista.ImagemUrl,
                            artista.Nome,
                            "ARTISTA  ·  Ver músicas",
                            true,
                            (s, e) => ArtistaSolicitado?.Invoke(artista)));
                }
                flpScroll.Controls.Add(flpArtistas);
            }

            // Sessao: Albuns em destaque.
            if (albuns != null && albuns.Count > 0)
            {
                var lblSecAlbuns = CriarTituloSecao("Álbuns em destaque",
                    "Novos sons para a sua próxima playlist");
                flpScroll.Controls.Add(lblSecAlbuns);

                flpAlbuns = CriarGradeDestaque();
                foreach (var album in albuns)
                {
                    flpAlbuns.Controls.Add(
                        CriarCardDestaque(
                            album.ImagemUrl,
                            album.Nome,
                            album.Artistas,
                            false,
                            (s, e) => AlbumSolicitado?.Invoke(
                                album.Album + " " + album.Artistas)));
                }
                flpScroll.Controls.Add(flpAlbuns);
            }

            if ((artistas == null || artistas.Count == 0)
                && (albuns == null || albuns.Count == 0))
            {
                flpScroll.Controls.Add(new Label
                {
                    Text = "Nenhum destaque disponivel no momento.",
                    ForeColor = Tema.TextoSecundario,
                    AutoSize = true,
                    Padding = new Padding(0, 20, 0, 0)
                });
            }

            flpScroll.ResumeLayout(true);
            ReposicionarSessoes();
            Tema.Aplicar(this);
        }

        private static Panel CriarTituloSecao(string titulo, string descricao)
        {
            var cabecalho = new Panel
            {
                Height = 64,
                Margin = new Padding(0, 12, 0, 6),
                BackColor = Color.Transparent
            };
            cabecalho.Controls.Add(new Label
            {
                Text = titulo,
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                Location = new Point(0, 2),
                Size = new Size(430, 32),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            });
            cabecalho.Controls.Add(new Label
            {
                Text = descricao,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 9F),
                Location = new Point(1, 37),
                Size = new Size(430, 20),
                AutoEllipsis = true,
                BackColor = Color.Transparent
            });
            return cabecalho;
        }

        private FlowLayoutPanel CriarGradeDestaque()
        {
            return new FlowLayoutPanel
            {
                WrapContents = true,
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 4)
            };
        }

        private Control CriarCardDestaque(
            string imagemUrl, string titulo, string subtitulo, bool artista, EventHandler clique)
        {
            var card = new Panel
            {
                Width = LarguraCard,
                Height = AlturaCard,
                Margin = new Padding(0, 0, EspacoCard, EspacoCard),
                BackColor = Tema.FundoElevado,
                Cursor = Cursors.Hand
            };
            Tema.Arredondar(card, 16);

            var picCapa = new PictureBox
            {
                Location = new Point(12, 12),
                Size = new Size(166, 166),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Tema.FundoCard,
                Cursor = Cursors.Hand
            };
            Tema.Arredondar(picCapa, 14);

            if (!string.IsNullOrWhiteSpace(imagemUrl))
            {
                try { picCapa.LoadAsync(imagemUrl); } catch { }
            }

            var lblTitulo = new Label
            {
                Text = titulo ?? "",
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Size = new Size(162, 25),
                AutoEllipsis = true,
                Location = new Point(14, 183),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };

            card.Controls.Add(picCapa);
            card.Controls.Add(lblTitulo);

            var lblSub = new Label
            {
                Text = subtitulo ?? "",
                ForeColor = artista ? Tema.Destaque : Tema.TextoSecundario,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(162, 19),
                AutoEllipsis = true,
                Location = new Point(14, 211),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblSub);

            // WinForms nao propaga Click dos filhos ao Panel: assina cada area.
            card.Click += clique;
            picCapa.Click += clique;
            lblTitulo.Click += clique;
            lblSub.Click += clique;

            foreach (Control alvo in new Control[] { card, picCapa, lblTitulo, lblSub })
            {
                alvo.MouseEnter += (s, e) => card.BackColor = Tema.FundoCard;
                alvo.MouseLeave += (s, e) =>
                {
                    if (!card.ClientRectangle.Contains(card.PointToClient(Cursor.Position)))
                        card.BackColor = Tema.FundoElevado;
                };
            }

            return card;
        }

        private void ReposicionarSessoes()
        {
            if (flpScroll == null || _ajustandoLayout)
                return;

            _ajustandoLayout = true;
            try
            {
                // Reserva a barra vertical para que o scroll nunca fique horizontal.
                int largura = Math.Max(180, flpScroll.ClientSize.Width
                    - flpScroll.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
                flpScroll.SuspendLayout();
                foreach (Control ctrl in flpScroll.Controls)
                {
                    ctrl.Width = largura;
                    if (ctrl == flpArtistas || ctrl == flpAlbuns)
                    {
                        var grade = (FlowLayoutPanel)ctrl;
                        int colunas = Math.Max(1, largura / (LarguraCard + EspacoCard));
                        int linhas = (grade.Controls.Count + colunas - 1) / colunas;
                        grade.Height = linhas * (AlturaCard + EspacoCard);
                    }
                    else if (ctrl is Panel painel)
                    {
                        foreach (Control filho in painel.Controls)
                        {
                            if (filho is Label)
                                filho.Width = Math.Max(120, largura - filho.Left - 16);
                        }
                        if (painel == pnlHero)
                            Tema.Arredondar(pnlHero, 18);
                    }
                }
                flpScroll.ResumeLayout(true);
                // O AutoScroll pode manter a largura anterior apos o Resize;
                // refaz a medicao para remover a barra horizontal residual.
                flpScroll.PerformLayout();
            }
            finally
            {
                _ajustandoLayout = false;
            }
        }

        private static string ObterSaudacao()
        {
            int hora = DateTime.Now.Hour;
            if (hora >= 5 && hora < 12) return "Bom dia";
            if (hora >= 12 && hora < 18) return "Boa tarde";
            return "Boa noite";
        }
    }
}
