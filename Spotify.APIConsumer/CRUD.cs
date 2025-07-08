using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Spotify.APIConsumer
{
        public static class CRUD<T>
        {
            public static string EndPoint { get; set; }

            public static List<T> GetAll()
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync(EndPoint).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        return JsonConvert.DeserializeObject<List<T>>(json);
                    }
                    else
                    {
                        throw new Exception($"Error: {response.StatusCode}");
                    }
                }
            }

            public static T GetById(int id)
            {
                using (var client = new HttpClient())
                {
                    var response = client.GetAsync($"{EndPoint}/{id}").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                    else
                    {
                        throw new Exception($"Error: {response.StatusCode}");
                    }
                }
            }

            public static T Create(T item)
            {
                using (var client = new HttpClient())
                {
                    var response = client.PostAsync(
                            EndPoint,
                            new StringContent(
                                JsonConvert.SerializeObject(item),
                                Encoding.UTF8,
                                "application/json"
                            )
                        ).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var json = response.Content.ReadAsStringAsync().Result;
                        return JsonConvert.DeserializeObject<T>(json);
                    }
                    else
                    {
                        throw new Exception($"Error: {response.StatusCode}");
                    }
                }
            }

            public static bool Update(int id, T item)
            {
                using (var client = new HttpClient())
                {
                    var response = client.PutAsync(
                            $"{EndPoint}/{id}",
                            new StringContent(
                                JsonConvert.SerializeObject(item),
                                Encoding.UTF8,
                                "application/json"
                            )
                        ).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception($"Error: {response.StatusCode}");
                    }
                }
            }

            public static bool Delete(int id)
            {
                using (var client = new HttpClient())
                {
                    var response = client.DeleteAsync($"{EndPoint}/{id}").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception($"Error: {response.StatusCode}");
                    }
                }
            }

        public static T UploadWithFile(
                  string titulo,
                  Stream fileStream,
                  string fileName,
                  string contentType,
                  string genero,
                  TimeSpan duracion,
                  int artistaId,
                  int albumId)
        {
            using var client = new HttpClient();
            // prepara multipart/form-data
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(titulo), "Titulo");
            content.Add(new StringContent(duracion.ToString()), "Duracion");
            content.Add(new StringContent(genero), "Genero");
            content.Add(new StringContent(artistaId.ToString()), "ArtistaId");
            content.Add(new StringContent(albumId.ToString()), "AlbumId");

            // archivo
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            content.Add(fileContent, "Archivo", fileName);

            // POST a /api/Canciones/upload
            var resp = client.PostAsync($"{EndPoint}/upload", content).Result;
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"Error: {resp.StatusCode}");

            var json = resp.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(json)!;
        }

    }
}
