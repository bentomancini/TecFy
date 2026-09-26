using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace WindowsFormsApp1
{
    public static class SpotifyService
    {
        // Chaves da API do Spotify, lidas do App.config (appSettings).
        // O fallback abaixo mantem compatibilidade com quem ainda nao tem
        // a chave no config.
        private static string ClientId
        {
            get { return LerOuFallback("SpotifyClientId", DefaultClientId); }
        }

        private static string ClientSecret
        {
            get { return LerOuFallback("SpotifyClientSecret", DefaultClientSecret); }
        }

        private const string DefaultClientId = "aa96c0975dd9468aa69fb214fac61e11";
        private const string DefaultClientSecret = "2a8406f87e26464eaebea4ffd358e044";

        private static string LerOuFallback(string chave, string padrao)
        {
            var valor = ConfigurationManager.AppSettings[chave];
            return string.IsNullOrWhiteSpace(valor) ? padrao : valor;
        }

        private static SpotifyClient _cliente;

        public class Faixa
        {
            public int? MusicaId { get; set; }
            public string Nome { get; set; }
            public string Artistas { get; set; }
            public string Album { get; set; }
            public string ImagemUrl { get; set; }
            public string UrlSpotify { get; set; }
            public int DuracaoSegundos { get; set; }
            public string PreviewUrl { get; set; }
        }

        public class Artista
        {
            public string Nome { get; set; }
            public string ImagemUrl { get; set; }
            public int Seguidores { get; set; }
            public List<string> Generos { get; set; }
        }

        public static bool Configurado
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ClientId)
                    && !ClientId.Contains("SEU_CLIENT_ID")
                    && !string.IsNullOrWhiteSpace(ClientSecret)
                    && !ClientSecret.Contains("SEU_CLIENT_SECRET");
            }
        }

        private static SpotifyClient ObterCliente()
        {
            if (!Configurado)
            {
                throw new InvalidOperationException(
                    "Credenciais do Spotify nao configuradas. Preencha ClientId e ClientSecret em SpotifyService.cs");
            }

            if (_cliente == null)
            {
                var config = SpotifyClientConfig.CreateDefault()
                    .WithAuthenticator(new ClientCredentialsAuthenticator(ClientId, ClientSecret));

                _cliente = new SpotifyClient(config);
            }

            return _cliente;
        }

        public static async Task<List<Faixa>> BuscarFaixasAsync(string termo, int limite = 10)
        {
            var cliente = ObterCliente();

            var resposta = await cliente.Search.Item(
                new SearchRequest(SearchRequest.Types.Track, termo) { Limit = limite });

            var faixas = new List<Faixa>();

            if (resposta?.Tracks?.Items == null)
                return faixas;

            foreach (var track in resposta.Tracks.Items.Cast<FullTrack>())
            {
                faixas.Add(new Faixa
                {
                    Nome = track.Name,
                    Artistas = string.Join(", ", track.Artists.Select(a => a.Name)),
                    Album = track.Album.Name,
                    ImagemUrl = track.Album.Images.FirstOrDefault()?.Url,
                    UrlSpotify = (track.ExternalUrls != null && track.ExternalUrls.ContainsKey("spotify"))
                        ? track.ExternalUrls["spotify"]
                        : null,
                    DuracaoSegundos = track.DurationMs > 0 ? track.DurationMs / 1000 : 0,
                    PreviewUrl = track.PreviewUrl
                });
            }

            string alvo = Normalizar(termo);

            faixas = faixas
                .OrderByDescending(f =>
                {
                    string nome = Normalizar(f.Nome);
                    string artista = Normalizar(f.Artistas);
                    int rank = 0;
                    if (!string.IsNullOrEmpty(nome) && nome.StartsWith(alvo, StringComparison.Ordinal))
                        rank = 5;
                    else if (!string.IsNullOrEmpty(artista) && artista.StartsWith(alvo, StringComparison.Ordinal))
                        rank = 4;
                    else if (!string.IsNullOrEmpty(nome) && nome.IndexOf(alvo, StringComparison.Ordinal) >= 0)
                        rank = 3;
                    else if (!string.IsNullOrEmpty(artista) && artista.IndexOf(alvo, StringComparison.Ordinal) >= 0)
                        rank = 2;
                    else if ((nome + " " + artista).Split(' ', '-', ',').Any(p =>
                        Normalizar(p).StartsWith(alvo, StringComparison.Ordinal)))
                        rank = 1;
                    return rank;
                })
                .ThenBy(f => f.Nome, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return faixas;
        }

        // Busca artistas na API do Spotify.
        public static async Task<List<Artista>> BuscarArtistasAsync(string termo, int limite = 10)
        {
            var cliente = ObterCliente();

            var resposta = await cliente.Search.Item(
                new SearchRequest(SearchRequest.Types.Artist, termo) { Limit = limite });

            var artistas = new List<Artista>();

            if (resposta?.Artists?.Items == null)
                return artistas;

            foreach (var artista in resposta.Artists.Items.Cast<FullArtist>())
            {
                artistas.Add(new Artista
                {
                    Nome = artista.Name,
                    ImagemUrl = artista.Images.FirstOrDefault()?.Url,
                    Seguidores = 0, // Spotify removeu o campo "followers" da API (sempre 0).
                    Generos = artista.Genres ?? new List<string>()
                });
            }

            // Ordena por relevancia em relacao ao termo digitado: prefixo exato,
            // depois contem-o-termo, e so depois os demais resultados.
            string alvo = Normalizar(termo);
            artistas = artistas
                .OrderByDescending(a =>
                {
                    string nome = Normalizar(a.Nome);
                    int rank = 0;
                    if (nome.StartsWith(alvo, StringComparison.Ordinal))
                        rank = 3;
                    else if (nome.IndexOf(alvo, StringComparison.Ordinal) >= 0)
                        rank = 2;
                    else if (nome.Split(' ', '-', ',').Any(p =>
                        Normalizar(p).StartsWith(alvo, StringComparison.Ordinal)))
                        rank = 1;
                    return rank;
                })
                .ThenBy(a => a.Nome, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return artistas;
        }

        // Nomes de artistas bem conhecidos usados para montar a grade de destaques.
        private static readonly string[] ArtistasFamosos =
        {
            "Anitta", "Marilia Mendonca", "Henrique & Juliano", "Jorge & Mateus",
            "Luan Santana", "Alok", "Gusttavo Lima", "Taylor Swift",
            "Billie Eilish", "Ed Sheeran", "Coldplay", "The Weeknd"
        };

        // Termos de albuns famosos (album + artista) para a grade de destaques.
        private static readonly string[] AlbunsFamosos =
        {
            "Future Nostalgia Dua Lipa",
            "After Hours The Weeknd",
            "Happier Than Ever Billie Eilish",
            "Versions of Me Anitta",
            "Todos os Cantos Marilia",
            "Henrique e Juliano Ao Vivo",
            "Midnights Taylor Swift",
            "Music of the Spheres Coldplay"
        };

        // Busca artistas em destaque. Usa entity=song para obter a capa de uma
        // musica do proprio artista (a iTunes nao retorna foto do artista).
        public static async Task<List<Artista>> BuscarArtistasDestaqueAsync(int limite = 8)
        {
            var tarefas = new List<Task<Artista>>();

            foreach (var nome in ArtistasFamosos)
            {
                tarefas.Add(BuscarArtistaDestaqueAsync(nome));
            }

            var resultados = await Task.WhenAll(tarefas);

            var lista = new List<Artista>();
            foreach (var artista in resultados)
            {
                if (artista != null && lista.Count < limite)
                    lista.Add(artista);
            }
            return lista;
        }

        private static async Task<Artista> BuscarArtistaDestaqueAsync(string nome)
        {
            try
            {
                string url = "https://itunes.apple.com/search?term="
                    + Uri.EscapeDataString(nome)
                    + "&entity=song&attribute=artistTerm&limit=10";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    string json = await client.GetStringAsync(url);

                    var resultado = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var resultados = resultado["results"] as Newtonsoft.Json.Linq.JArray;

                    if (resultados == null || resultados.Count == 0)
                        return null;

                    var item = resultados.FirstOrDefault(r =>
                        Normalizar(Convert.ToString(r["artistName"])) == Normalizar(nome))
                        ?? resultados.FirstOrDefault(r =>
                            NomeCorresponde(Convert.ToString(r["artistName"]), nome));
                    if (item == null)
                        return null;

                    string nomeArtista = Convert.ToString(item["artistName"]);
                    if (string.IsNullOrWhiteSpace(nomeArtista))
                        return null;

                    string imagem = item["artworkUrl100"] == null
                        ? null
                        : Convert.ToString(item["artworkUrl100"]);
                    if (!string.IsNullOrEmpty(imagem))
                        imagem = imagem.Replace("100x100bb", "400x400bb");

                    return new Artista
                    {
                        Nome = Normalizar(nomeArtista) == Normalizar(nome) ? nomeArtista : nome,
                        ImagemUrl = imagem ?? ""
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        // Busca albuns em destaque na iTunes, de forma paralela.
        public static async Task<List<Faixa>> BuscarAlbunsDestaqueAsync(int limite = 6)
        {
            var tarefas = new List<Task<Faixa>>();

            foreach (var termo in AlbunsFamosos)
            {
                tarefas.Add(BuscarAlbumDestaqueAsync(termo));
            }

            var resultados = await Task.WhenAll(tarefas);

            var lista = new List<Faixa>();
            foreach (var album in resultados)
            {
                if (album != null && lista.Count < limite)
                    lista.Add(album);
            }
            return lista;
        }

        private static async Task<Faixa> BuscarAlbumDestaqueAsync(string termo)
        {
            try
            {
                string url = "https://itunes.apple.com/search?term="
                    + Uri.EscapeDataString(termo)
                    + "&entity=album&attribute=albumTerm&limit=1";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    string json = await client.GetStringAsync(url);

                    var resultado = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var resultados = resultado["results"] as Newtonsoft.Json.Linq.JArray;

                    if (resultados == null || resultados.Count == 0)
                        return null;

                    var item = resultados[0];
                    string album = item["collectionName"] == null
                        ? null
                        : Convert.ToString(item["collectionName"]);
                    string artista = item["artistName"] == null
                        ? null
                        : Convert.ToString(item["artistName"]);

                    if (string.IsNullOrWhiteSpace(album))
                        return null;

                    string imagem = item["artworkUrl100"] == null
                        ? null
                        : Convert.ToString(item["artworkUrl100"]);
                    if (!string.IsNullOrEmpty(imagem))
                        imagem = imagem.Replace("100x100bb", "400x400bb");

                    return new Faixa
                    {
                        Nome = album,
                        Album = album,
                        Artistas = artista ?? "",
                        ImagemUrl = imagem ?? ""
                    };
                }
            }
            catch
            {
                return null;
            }
        }

        // Normaliza texto para comparacao sem diferenciar maiusculas/acentos.
        private static string Normalizar(string texto)
        {
            if (string.IsNullOrEmpty(texto))
                return "";
            texto = texto.Trim().ToLowerInvariant();
            string marcado = texto.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char c in marcado)
            {
                System.Globalization.UnicodeCategory uc =
                    System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString();
        }

        // Busca musicas na iTunes Search API, usada como fallback quando
        // a API do Spotify esta instavel (erros 502) ou sem resultados.
        public static async Task<List<Faixa>> BuscarFaixasItunesAsync(string termo, int limite = 10)
        {
            var faixas = new List<Faixa>();

            try
            {
                if (string.IsNullOrWhiteSpace(termo))
                    return faixas;

                string url = "https://itunes.apple.com/search?term="
                    + Uri.EscapeDataString(termo)
                    + "&entity=song&limit=" + limite;

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    string json = await client.GetStringAsync(url);

                    var resultado = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var resultados = resultado["results"] as Newtonsoft.Json.Linq.JArray;

                    if (resultados == null)
                        return faixas;

                    foreach (var item in resultados)
                    {
                        string nome = Convert.ToString(item["trackName"]);
                        if (string.IsNullOrWhiteSpace(nome))
                            continue;

                        string preview = Convert.ToString(item["previewUrl"]);
                        string capa = Convert.ToString(item["artworkUrl100"]);
                        if (!string.IsNullOrEmpty(capa))
                        {
                            capa = capa.Replace("100x100bb", "300x300bb");
                        }

                        faixas.Add(new Faixa
                        {
                            Nome = nome,
                            Artistas = Convert.ToString(item["artistName"]),
                            Album = item["collectionName"] == null
                                ? null
                                : Convert.ToString(item["collectionName"]),
                            ImagemUrl = string.IsNullOrEmpty(capa) ? null : capa,
                            UrlSpotify = null,
                            DuracaoSegundos = item["trackTimeMillis"] != null
                                ? (int)(Convert.ToInt64(item["trackTimeMillis"]) / 1000)
                                : 0,
                            PreviewUrl = string.IsNullOrEmpty(preview) ? null : preview
                        });
                    }
                }
            }
            catch
            {
            }

            return faixas;
        }

        // true se o nome do artista retornado pela iTunes corresponde ao termo buscado.
        // Permissivo para aceitar nomes compostos, parcerias e duplas
        // (ex.: "Henrique & Juliano" casa com "Henrique e Juliano"; "Anitta, Zaac & Maejor" casa com "Anitta").
        private static bool NomeCorresponde(string nomeRetornado, string termoBuscado)
        {
            if (string.IsNullOrWhiteSpace(nomeRetornado))
                return false;

            if (string.IsNullOrWhiteSpace(termoBuscado))
                return true;

            string nome = Normalizar(nomeRetornado);
            string termo = Normalizar(termoBuscado);

            // 1. Igualdade exata (trata "e"/"&", acentos e maiusculas via Normalizar).
            if (nome == termo)
                return true;

            // 2. Um contem o outro (inteiro).
            if (nome.IndexOf(termo, StringComparison.Ordinal) >= 0)
                return true;
            // 3. Em nomes compostos, todas as palavras distintivas precisam
            // aparecer (ex.: Henrique & Juliano nao e Henrique & Diego).
            var partesNome = nome.Split(new[] { ' ', ',', '&', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var partesTermo = termo.Split(new[] { ' ', ',', '&', '-' }, StringSplitOptions.RemoveEmptyEntries);

            var distintivas = partesTermo.Where(p => p.Length >= 4).ToList();
            return distintivas.Count > 0 && distintivas.All(partesNome.Contains);
        }

        // Busca as principais musicas de um artista na iTunes Search API.
        // A iTunes permite buscar pelo nome do artista e traz as musicas dele
        // (com preview de 30s para tocar).
        public static async Task<List<Faixa>> BuscarTopMusicasArtistaAsync(string artista, int limite = 5)
        {
            var faixas = new List<Faixa>();
            var nomesEncontrados = new HashSet<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(artista))
                    return faixas;

                string url = "https://itunes.apple.com/search?term="
                    + Uri.EscapeDataString(artista)
                    + "&entity=song&attribute=artistTerm&limit=" + (limite * 4);

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    string json = await client.GetStringAsync(url);

                    var resultado = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var resultados = resultado["results"] as Newtonsoft.Json.Linq.JArray;

                    if (resultados == null)
                        return faixas;

                    foreach (var item in resultados)
                    {
                        string nome = Convert.ToString(item["trackName"]);
                        string nomeArtista = Convert.ToString(item["artistName"]);
                        if (string.IsNullOrWhiteSpace(nome))
                            continue;
                        if (!NomeCorresponde(nomeArtista, artista))
                            continue;
                        if (!nomesEncontrados.Add(Normalizar(nome)))
                            continue;

                        string preview = Convert.ToString(item["previewUrl"]);
                        string capa = Convert.ToString(item["artworkUrl100"]);
                        if (!string.IsNullOrEmpty(capa))
                            capa = capa.Replace("100x100bb", "300x300bb");

                        faixas.Add(new Faixa
                        {
                            Nome = nome,
                            Artistas = nomeArtista,
                            Album = item["collectionName"] == null
                                ? null
                                : Convert.ToString(item["collectionName"]),
                            ImagemUrl = string.IsNullOrEmpty(capa) ? null : capa,
                            UrlSpotify = null,
                            DuracaoSegundos = item["trackTimeMillis"] != null
                                ? (int)(Convert.ToInt64(item["trackTimeMillis"]) / 1000)
                                : 0,
                            PreviewUrl = string.IsNullOrEmpty(preview) ? null : preview
                        });
                    }
                }
            }
            catch
            {
            }

            return faixas.Take(limite).ToList();
        }

        // Busca um preview (30s) na iTunes Search API como alternativa,
        // porque o Spotify descontinuou o preview_url para a maioria das musicas.
        public static async Task<string> BuscarPreviaItunesAsync(string titulo, string artista)
        {
            try
            {
                var termo = (titulo ?? "") + " " + (artista ?? "");
                if (string.IsNullOrWhiteSpace(termo))
                    return null;

                string url = "https://itunes.apple.com/search?term="
                    + Uri.EscapeDataString(termo)
                    + "&entity=song&limit=5";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    string json = await client.GetStringAsync(url);

                    var resultado = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var resultados = resultado["results"] as Newtonsoft.Json.Linq.JArray;

                    if (resultados == null || resultados.Count == 0)
                        return null;

                    foreach (var item in resultados)
                    {
                        string preview = item["previewUrl"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(preview))
                            return preview;
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        // Busca a URL da capa de uma faixa na iTunes Search API (fallback de capa).
        public static async Task<string> BuscarCapaItunesAsync(string titulo, string artista)
        {
            try
            {
                var termo = (titulo ?? "") + " " + (artista ?? "");
                if (string.IsNullOrWhiteSpace(termo))
                    return null;

                string url = "https://itunes.apple.com/search?term="
                    + Uri.EscapeDataString(termo)
                    + "&entity=song&limit=5";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    string json = await client.GetStringAsync(url);

                    var resultado = Newtonsoft.Json.Linq.JObject.Parse(json);
                    var resultados = resultado["results"] as Newtonsoft.Json.Linq.JArray;

                    if (resultados == null || resultados.Count == 0)
                        return null;

                    foreach (var item in resultados)
                    {
                        string capa = item["artworkUrl100"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(capa))
                            return capa.Replace("100x100bb", "300x300bb");
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
