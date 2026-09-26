using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class ControleArtistas : UserControl
    {
        public ControleArtistas(int usuarioId)
        {
            _usuarioId = usuarioId;
            Inicializar();
        }

        // Disparado quando o usuario quer tocar uma das musicas do artista.
        public event Action<SpotifyService.Faixa> MusicaSolicitada;

        private int _usuarioId;

        private Button btnFavoritarSelecionado;
        private Button btnFavoritos;
        private Panel _pnlTopo;
        private ListView lstArtistas;
        private ImageList _imagens;

        // Painel de detalhe do artista.
        private ScrollableControl pnlDetalhe;
        private PictureBox picArtista;
        private Label lblNomeArtista;
        private Label lblInfoArtista;
        private FlowLayoutPanel flpTopMusicas;
        private Button btnVoltar;
        private Button btnFavorito;
        private SpotifyService.Artista _artistaAtual;
        private bool _exibindoDetalhe;
        private int _versaoTopMusicas;

        private void Inicializar()
        {
            BackColor = Color.FromArgb(13, 7, 20);
            Font = new Font("Segoe UI", 9F);

            // A busca de artistas usa o campo principal do Form2.
            _pnlTopo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Tema.FundoPainel
            };

            _pnlTopo.Controls.Add(new Label
            {
                Text = "Artistas",
                Location = new Point(12, 5),
                Size = new Size(210, 30),
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                BackColor = Color.Transparent
            });
            _pnlTopo.Controls.Add(new Label
            {
                Text = "Busque na barra acima ou explore seus favoritos",
                Location = new Point(13, 37),
                Size = new Size(365, 20),
                AutoEllipsis = true,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent
            });

            btnFavoritarSelecionado = NovoBotaoTopo("♡ Favoritar", Tema.FundoCard, 108, BtnFavoritarSelecionado_Click);
            btnFavoritos = NovoBotaoTopo("♥ Favoritos", Tema.FundoCard, 104, BtnFavoritos_Click);

            _pnlTopo.Controls.Add(btnFavoritarSelecionado);
            _pnlTopo.Controls.Add(btnFavoritos);
            ReposicionarBotoesTopo();

            Resize += (s, e) => ReposicionarBotoesTopo();

            // ---- Lista de artistas ----
            _imagens = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(68, 68)
            };

            lstArtistas = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(28, 16, 42),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12.5F),
                SmallImageList = _imagens,
                HeaderStyle = ColumnHeaderStyle.None
            };
            lstArtistas.Columns.Add("Artista", 360);
            lstArtistas.DoubleClick += LstArtistas_DoubleClick;
            lstArtistas.SelectedIndexChanged += LstArtistas_SelectedIndexChanged;

            Controls.Add(lstArtistas);

            CriarPainelDetalhe();
            Controls.Add(pnlDetalhe);
            pnlDetalhe.Visible = false;

            // O DockStyle.Top deve vir depois dos paineis Fill para reservar espaco.
            Controls.Add(_pnlTopo);

            Load += (s, e) =>
            {
                if (!_exibindoDetalhe)
                    BuscarArtistas("Brasil");
            };
        }

        private Button NovoBotaoTopo(string texto, Color cor, int largura, EventHandler clique)
        {
            var botao = new Button
            {
                Text = texto,
                Size = new Size(largura, 26),
                BackColor = cor,
                ForeColor = Tema.Texto,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            botao.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(botao, 13);
            botao.Click += clique;
            return botao;
        }

        private void ReposicionarBotoesTopo()
        {
            if (btnFavoritos == null || btnFavoritarSelecionado == null)
                return;

            int direita = Width - 12;
            btnFavoritos.Location = new Point(direita - btnFavoritos.Width, 16);
            btnFavoritarSelecionado.Location = new Point(btnFavoritos.Left - btnFavoritarSelecionado.Width - 8, 16);
        }

        private void CriarPainelDetalhe()
        {
            pnlDetalhe = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 11, 30),
                Visible = false
            };

            // Barra superior com o botao voltar (sempre visivel).
            var pnlTopoDetalhe = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Tema.FundoPainel
            };

            btnVoltar = new Button
            {
                Text = "< Voltar",
                Location = new Point(10, 8),
                Size = new Size(110, 32),
                BackColor = Tema.FundoCard,
                ForeColor = Tema.Texto,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnVoltar.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnVoltar, 12);
            btnVoltar.Click += (s, e) => MostrarLista();
            pnlTopoDetalhe.Controls.Add(btnVoltar);
            pnlTopoDetalhe.Controls.Add(new Label
            {
                Text = "PERFIL DO ARTISTA",
                Location = new Point(136, 15),
                Size = new Size(190, 22),
                ForeColor = Tema.Destaque,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.Transparent
            });

            // Usa um TableLayoutPanel (grid 2 colunas) para separar o card do artista
            // (esquerda) das musicas (direita), sem sobreposicao por dock.
            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(20, 11, 30)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Coluna 0: card do artista (foto + nome + info + favoritar).
            var pnlEsquerda = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Tema.FundoElevado
            };
            pnlEsquerda.Resize += (s, e) => Tema.Arredondar(pnlEsquerda, 16);

            picArtista = new PictureBox
            {
                Location = new Point(34, 14),
                Size = new Size(172, 172),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Tema.FundoCard
            };
            Tema.Arredondar(picArtista, 16);
            pnlEsquerda.Controls.Add(picArtista);

            lblNomeArtista = new Label
            {
                Location = new Point(8, 193),
                Size = new Size(224, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            pnlEsquerda.Controls.Add(lblNomeArtista);

            lblInfoArtista = new Label
            {
                Location = new Point(8, 230),
                Size = new Size(224, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 10F),
                BackColor = Color.Transparent
            };
            pnlEsquerda.Controls.Add(lblInfoArtista);

            btnFavorito = new Button
            {
                Location = new Point(34, 276),
                Size = new Size(172, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnFavorito.FlatAppearance.BorderSize = 0;
            Tema.Arredondar(btnFavorito, 15);
            btnFavorito.Click += BtnFavorito_Click;
            pnlEsquerda.Controls.Add(btnFavorito);

            grid.Controls.Add(pnlEsquerda, 0, 0);

            // Coluna 1: as principais musicas do artista.
            var pnlDireita = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 11, 30)
            };

            var lblMusicas = new Label
            {
                Dock = DockStyle.Top,
                Height = 30,
                Text = "  Músicas em destaque",
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                BackColor = Color.FromArgb(20, 11, 30)
            };

            flpTopMusicas = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(8, 8, 8, 8),
                BackColor = Color.FromArgb(20, 11, 30)
            };

            // Acompanha o tamanho da janela: repassa a largura nova a cada linha.
            flpTopMusicas.Resize += (s, e) =>
            {
                if (flpTopMusicas.ClientSize.Width <= 0)
                    return;
                int larguraNova = Math.Max(160, flpTopMusicas.ClientSize.Width
                    - flpTopMusicas.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);
                foreach (Control controle in flpTopMusicas.Controls)
                {
                    if (controle.Width != larguraNova)
                        controle.Width = larguraNova;
                }
            };

            // Ordem IMPORTANTE no WinForms: o dock e processado na ordem inversa
            // da adicao. Adiciona primeiro o Fill (flp) e DEPOIS o Top (lblMusicas),
            // para o label reservar o topo e o flp preencher abaixo dele.
            pnlDireita.Controls.Add(flpTopMusicas);
            pnlDireita.Controls.Add(lblMusicas);

            grid.Controls.Add(pnlDireita, 1, 0);

            pnlDetalhe.Controls.Add(grid);
            // O DockStyle.Top deve ser adicionado depois do Fill para reservar a barra.
            pnlDetalhe.Controls.Add(pnlTopoDetalhe);
        }

        private void MostrarLista()
        {
            _versaoTopMusicas++;
            _exibindoDetalhe = false;
            _pnlTopo.Visible = true;
            lstArtistas.Visible = true;
            pnlDetalhe.Visible = false;
        }

        // Permite abrir o detalhe de um artista diretamente (ex.: vindo da home).
        public void ExibirLista()
        {
            MostrarLista();
        }

        public void Pesquisar(string termo)
        {
            MostrarLista();
            BuscarArtistas(string.IsNullOrWhiteSpace(termo) ? "Brasil" : termo.Trim());
        }

        public void ExibirArtista(SpotifyService.Artista artista)
        {
            if (artista == null)
                return;
            MostrarDetalhe(artista);
        }

        private void MostrarDetalhe(SpotifyService.Artista artista)
        {
            _exibindoDetalhe = true;
            _pnlTopo.Visible = false;
            lstArtistas.Visible = false;
            pnlDetalhe.Visible = true;

            _artistaAtual = artista;
            lblNomeArtista.Text = artista.Nome;

            string generos = artista.Generos != null && artista.Generos.Count > 0
                ? string.Join(", ", artista.Generos.Take(3))
                : "";
            string seguidores = artista.Seguidores > 0
                ? artista.Seguidores.ToString("N0") + " seguidores"
                : "";
            lblInfoArtista.Text = string.Join(Environment.NewLine,
                new[]
                {
                    !string.IsNullOrWhiteSpace(generos) ? generos : "",
                    !string.IsNullOrWhiteSpace(seguidores) ? seguidores : ""
                }.Where(s => !string.IsNullOrWhiteSpace(s)));

            // Carregamento assincrono: nao bloqueia a exibicao das musicas.
            picArtista.ImageLocation = null;
            picArtista.Image = null;
            if (!string.IsNullOrWhiteSpace(artista.ImagemUrl))
            {
                try { picArtista.LoadAsync(artista.ImagemUrl); } catch { }
            }

            CarregarTopMusicas(artista.Nome);

            AtualizarBotaoFavorito();
        }

        private async void CarregarTopMusicas(string nomeArtista)
        {
            int versao = ++_versaoTopMusicas;
            flpTopMusicas.Controls.Clear();
            flpTopMusicas.Controls.Add(new Label
            {
                Text = "Carregando músicas de " + nomeArtista + "...",
                ForeColor = Tema.TextoSecundario,
                AutoSize = true,
                Margin = new Padding(8, 12, 0, 0)
            });

            List<SpotifyService.Faixa> top = null;
            try { top = await SpotifyService.BuscarTopMusicasArtistaAsync(nomeArtista, 10); }
            catch { }

            if (IsDisposed || versao != _versaoTopMusicas)
                return;

            flpTopMusicas.Controls.Clear();

            if (top == null || top.Count == 0)
            {
                var msg = new Label
                {
                    Text = "Nenhuma música encontrada para este artista agora.",
                    ForeColor = Tema.TextoSecundario,
                    AutoSize = true,
                    Padding = new Padding(0, 6, 0, 0)
                };
                flpTopMusicas.Controls.Add(msg);
                Tema.Aplicar(flpTopMusicas);
                return;
            }

            int numero = 1;
            foreach (var faixa in top)
            {
                var item = CriarLinhaMusica(numero, faixa);
                flpTopMusicas.Controls.Add(item);
                numero++;
            }

            Tema.Aplicar(flpTopMusicas);
        }

        private Control CriarLinhaMusica(int numero, SpotifyService.Faixa faixa)
        {
            int larguraLinha = Math.Max(160, flpTopMusicas.ClientSize.Width
                - flpTopMusicas.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 4);

            var pnl = new Panel
            {
                Size = new Size(larguraLinha, 70),
                BackColor = Tema.FundoElevado,
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };
            Tema.Arredondar(pnl, 12);

            var lblNumero = new Label
            {
                Text = numero.ToString("00"),
                Location = new Point(9, 24),
                Size = new Size(26, 22),
                ForeColor = Tema.Destaque,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            pnl.Controls.Add(lblNumero);

            var capa = new PictureBox
            {
                Size = new Size(54, 54),
                Location = new Point(38, 8),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Tema.FundoCard,
                Cursor = Cursors.Hand
            };
            Tema.Arredondar(capa, 9);
            if (!string.IsNullOrWhiteSpace(faixa.ImagemUrl))
            {
                try { capa.LoadAsync(faixa.ImagemUrl); } catch { }
            }
            pnl.Controls.Add(capa);

            var duracao = new Label
            {
                Text = FormatarDuracao(faixa.DuracaoSegundos),
                Location = new Point(larguraLinha - 53, 25),
                Size = new Size(48, 20),
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            pnl.Controls.Add(duracao);

            var lblTitulo = new Label
            {
                Text = faixa.Nome ?? "",
                Location = new Point(102, 13),
                Size = new Size(Math.Max(55, larguraLinha - 165), 23),
                AutoEllipsis = true,
                ForeColor = Tema.Texto,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            pnl.Controls.Add(lblTitulo);

            var lblAlbum = new Label
            {
                Text = faixa.Album ?? faixa.Artistas ?? "",
                Location = new Point(102, 39),
                Size = new Size(Math.Max(55, larguraLinha - 165), 19),
                AutoEllipsis = true,
                ForeColor = Tema.TextoSecundario,
                Font = new Font("Segoe UI", 8.5F),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            pnl.Controls.Add(lblAlbum);

            pnl.Click += (s, e) => MusicaSolicitada?.Invoke(faixa);
            foreach (Control c in pnl.Controls)
                c.Click += (s, e) => MusicaSolicitada?.Invoke(faixa);

            pnl.Resize += (s, e) =>
            {
                duracao.Left = pnl.Width - duracao.Width - 6;
                lblTitulo.Width = Math.Max(55, duracao.Left - lblTitulo.Left - 8);
                lblAlbum.Width = lblTitulo.Width;
                Tema.Arredondar(pnl, 12);
            };

            foreach (Control alvo in new Control[] { pnl, lblNumero, capa, duracao, lblTitulo, lblAlbum })
            {
                alvo.MouseEnter += (s, e) => pnl.BackColor = Tema.FundoCard;
                alvo.MouseLeave += (s, e) =>
                {
                    if (!pnl.ClientRectangle.Contains(pnl.PointToClient(Cursor.Position)))
                        pnl.BackColor = Tema.FundoElevado;
                };
            }

            return pnl;
        }

        private void LstArtistas_DoubleClick(object sender, EventArgs e)
        {
            if (lstArtistas.SelectedItems.Count == 0)
                return;

            int idx = lstArtistas.SelectedItems[0].Index;
            if (idx < 0 || idx >= _resultadosBusca.Count)
                return;

            MostrarDetalhe(_resultadosBusca[idx]);
        }

        // Clique simples/teclado tambem abre o detalhe do artista selecionado.
        private void LstArtistas_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_carregando || !lstArtistas.Visible || pnlDetalhe.Visible)
                return;
            if (lstArtistas.SelectedItems.Count == 0)
                return;

            int idx = lstArtistas.SelectedItems[0].Index;
            if (idx < 0 || idx >= _resultadosBusca.Count)
                return;

            MostrarDetalhe(_resultadosBusca[idx]);
        }

        private List<SpotifyService.Artista> _resultadosBusca = new List<SpotifyService.Artista>();
        private bool _carregando;
        private int _versaoBusca;

        private async void AtualizarBotaoFavorito()
        {
            var artista = _artistaAtual;
            if (artista == null || _usuarioId <= 0)
            {
                btnFavorito.Visible = false;
                return;
            }

            btnFavorito.Visible = false;
            bool ehFavorito = await System.Threading.Tasks.Task.Run(
                () => ArtistaDAO.EhFavorito(_usuarioId, artista.Nome));
            if (IsDisposed || !_exibindoDetalhe || _artistaAtual != artista)
                return;

            btnFavorito.Text = ehFavorito ? "♥ Desfavoritar" : "♡ Favoritar";
            btnFavorito.BackColor = ehFavorito ? Color.FromArgb(200, 40, 70) : Color.FromArgb(124, 58, 237);
            btnFavorito.ForeColor = Color.White;
            btnFavorito.Visible = true;
        }

        private void BtnFavorito_Click(object sender, EventArgs e)
        {
            if (_usuarioId <= 0 || _artistaAtual == null)
                return;

            bool ehFavorito = ArtistaDAO.EhFavorito(_usuarioId, _artistaAtual.Nome);
            var resultado = ehFavorito
                ? ArtistaDAO.RemoverFavorito(_usuarioId, _artistaAtual.Nome)
                : ArtistaDAO.Favoritar(_usuarioId, _artistaAtual);

            if (resultado.Ok)
            {
                AtualizarBotaoFavorito();
            }
            else
            {
                MessageBox.Show(resultado.Erro, "Artistas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnFavoritos_Click(object sender, EventArgs e)
        {
            MostrarFavoritos();
        }

        // Favorita o artista selecionado na lista, sem abrir o detalhe.
        private void BtnFavoritarSelecionado_Click(object sender, EventArgs e)
        {
            if (_usuarioId <= 0)
            {
                MessageBox.Show("Entre no app para favoritar artistas.",
                    "Artistas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (lstArtistas.SelectedItems.Count == 0)
            {
                MessageBox.Show("Selecione um artista na lista para favoritar.",
                    "Artistas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idx = lstArtistas.SelectedItems[0].Index;
            if (idx < 0 || idx >= _resultadosBusca.Count)
                return;

            var artista = _resultadosBusca[idx];
            if (artista == null || string.IsNullOrWhiteSpace(artista.Nome))
                return;

            var resultado = ArtistaDAO.Favoritar(_usuarioId, artista);
            if (resultado.Ok)
            {
                MessageBox.Show("Artista adicionado aos favoritos: " + artista.Nome,
                    "Artistas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(resultado.Erro, "Artistas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async void MostrarFavoritos()
        {
            MostrarLista();
            int versao = ++_versaoBusca;
            if (_usuarioId <= 0)
            {
                MessageBox.Show("Entre no app para ver seus artistas favoritos.",
                    "Artistas", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var favoritos = await System.Threading.Tasks.Task.Run(() => ArtistaDAO.ListarFavoritos(_usuarioId));
            if (IsDisposed || _exibindoDetalhe || versao != _versaoBusca)
                return;

            _carregando = true;
            _imagens.Images.Clear();
            lstArtistas.Items.Clear();
            pnlDetalhe.Visible = false;
            lstArtistas.Visible = true;

            if (favoritos == null || favoritos.Count == 0)
            {
                AdicionarMensagem("Voce ainda nao tem artistas favoritos.");
                _carregando = false;
                return;
            }

            _resultadosBusca.Clear();
            _resultadosBusca.AddRange(favoritos);

            foreach (var artista in favoritos)
            {
                if (IsDisposed || versao != _versaoBusca)
                    return;
                var item = new ListViewItem(artista.Nome)
                {
                    ImageIndex = -1
                };

                if (!string.IsNullOrWhiteSpace(artista.ImagemUrl))
                {
                    try
                    {
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            var dados = await client.GetByteArrayAsync(artista.ImagemUrl);
                            if (IsDisposed || versao != _versaoBusca)
                                return;
                            using (var ms = new System.IO.MemoryStream(dados))
                            using (var imagem = Image.FromStream(ms))
                            {
                                _imagens.Images.Add(CriarMiniaturaArredondada(imagem, _imagens.ImageSize.Width));
                                item.ImageIndex = _imagens.Images.Count - 1;
                            }
                        }
                    }
                    catch
                    {
                        item.ImageIndex = -1;
                    }
                }

                lstArtistas.Items.Add(item);
            }

            _carregando = false;
        }

        private void AdicionarMensagem(string mensagem)
        {
            _imagens.Images.Clear();
            lstArtistas.Items.Clear();
            lstArtistas.Items.Add(new ListViewItem(" " + mensagem) { ForeColor = Color.White });
        }

        // O ListView nao usa Region em suas imagens; arredondamos os pixels
        // antes de colocar a foto quadrada no ImageList.
        private static Bitmap CriarMiniaturaArredondada(Image imagem, int tamanho)
        {
            var miniatura = new Bitmap(tamanho, tamanho);
            const int raio = 10;
            int diametro = raio * 2;
            using (var g = Graphics.FromImage(miniatura))
            using (var caminho = new GraphicsPath())
            {
                caminho.AddArc(0, 0, diametro, diametro, 180, 90);
                caminho.AddArc(tamanho - diametro - 1, 0, diametro, diametro, 270, 90);
                caminho.AddArc(tamanho - diametro - 1, tamanho - diametro - 1,
                    diametro, diametro, 0, 90);
                caminho.AddArc(0, tamanho - diametro - 1, diametro, diametro, 90, 90);
                caminho.CloseFigure();

                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SetClip(caminho);
                int lado = Math.Min(imagem.Width, imagem.Height);
                var origem = new Rectangle((imagem.Width - lado) / 2,
                    (imagem.Height - lado) / 2, lado, lado);
                g.DrawImage(imagem, new Rectangle(0, 0, tamanho, tamanho),
                    origem, GraphicsUnit.Pixel);
            }
            return miniatura;
        }

        private async void BuscarArtistas(string termo)
        {
            int versao = ++_versaoBusca;
            if (string.IsNullOrWhiteSpace(termo))
            {
                AdicionarMensagem("Digite o nome de um artista para buscar.");
                return;
            }

            if (!SpotifyService.Configurado)
            {
                AdicionarMensagem("Credenciais do Spotify nao configuradas em SpotifyService.cs.");
                return;
            }

            AdicionarMensagem("Buscando artistas no Spotify...");

            try
            {
                var artistas = await SpotifyService.BuscarArtistasAsync(termo);
                if (IsDisposed || versao != _versaoBusca)
                    return;
                _resultadosBusca.Clear();
                _resultadosBusca.AddRange(artistas ?? new List<SpotifyService.Artista>());

                _carregando = true;
                _imagens.Images.Clear();
                lstArtistas.Items.Clear();

                if (artistas == null || artistas.Count == 0)
                {
                    AdicionarMensagem("Nenhum artista encontrado para \"" + termo + "\".");
                    _carregando = false;
                    return;
                }

                foreach (var artista in artistas)
                {
                    if (IsDisposed || versao != _versaoBusca)
                        return;
                    string generos = artista.Generos != null && artista.Generos.Count > 0
                        ? string.Join(", ", artista.Generos.Take(3))
                        : "";
                    string seguidores = artista.Seguidores > 0
                        ? artista.Seguidores.ToString("N0") + " seguidores"
                        : "";
                    string info = string.Join("   .   ", new[] { generos, seguidores }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                    var item = new ListViewItem(artista.Nome + (string.IsNullOrWhiteSpace(info) ? "" : "   |   " + info))
                    {
                        ImageIndex = -1
                    };

                    if (!string.IsNullOrWhiteSpace(artista.ImagemUrl))
                    {
                        try
                        {
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                var dados = await client.GetByteArrayAsync(artista.ImagemUrl);
                                if (IsDisposed || versao != _versaoBusca)
                                    return;
                                using (var ms = new System.IO.MemoryStream(dados))
                                using (var imagem = Image.FromStream(ms))
                                {
                                    _imagens.Images.Add(CriarMiniaturaArredondada(imagem, _imagens.ImageSize.Width));
                                    item.ImageIndex = _imagens.Images.Count - 1;
                                }
                            }
                        }
                        catch
                        {
                            item.ImageIndex = -1;
                        }
                    }

                    lstArtistas.Items.Add(item);
                }

                _carregando = false;
                if (!_exibindoDetalhe)
                {
                    lstArtistas.Visible = true;
                    pnlDetalhe.Visible = false;
                }
            }
            catch (Exception ex)
            {
                if (IsDisposed || versao != _versaoBusca)
                    return;
                _carregando = false;
                _imagens.Images.Clear();
                lstArtistas.Items.Clear();
                AdicionarMensagem("Erro ao buscar artistas: " + ex.Message);
            }
        }

        private string FormatarDuracao(int segundos)
        {
            if (segundos <= 0)
                return "--:--";
            int min = segundos / 60;
            int sec = segundos % 60;
            return min + ":" + sec.ToString("00");
        }
    }
}
