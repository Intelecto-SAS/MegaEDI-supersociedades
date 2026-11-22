using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Edi360.Tests;

public class AcceptanceTests : IClassFixture<DemoFactory>
{
    private readonly DemoFactory _factory;
    public AcceptanceTests(DemoFactory factory) => _factory = factory;

    private HttpClient Client() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        HandleCookies = true,
        AllowAutoRedirect = false
    });

    private async Task<HttpClient> Login(string user)
    {
        var c = Client();
        var res = await c.PostAsJsonAsync("/api/auth/login", new { userName = user, password = "demo" });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        return c;
    }

    [Fact]
    public async Task Salud()
    {
        var res = await Client().GetAsync("/api/salud");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task LoginYSesion()
    {
        var c = await Login("laura.jimenez");
        var me = await c.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("Laura Jiménez Soto", me.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task CambioDePerfilRequiereDemo()
    {
        var c = await Login("laura.jimenez");
        var res = await c.PostAsJsonAsync("/api/demo/impersonar", new { userId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AdminCambiaDeFuncionario()
    {
        var c = await Login("valeria.santamaria");
        var oficiales = await c.GetFromJsonAsync<JsonElement>("/api/auth/oficiales");
        var felipe = oficiales.EnumerateArray().First(o => o.GetProperty("userName").GetString() == "felipe.ardila");
        var res = await c.PostAsJsonAsync("/api/demo/impersonar", new { userId = felipe.GetProperty("id").GetGuid() });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        var me = await c.GetFromJsonAsync<JsonElement>("/api/auth/me");
        Assert.Equal("Felipe Ardila Cruz", me.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task RadicadorNoApruebaProvidencia()
    {
        var admin = await Login("valeria.santamaria");
        var list = await admin.GetFromJsonAsync<JsonElement>("/api/expedientes?type=Insolvencia&size=1");
        var id = list.GetProperty("items")[0].GetProperty("id").GetGuid();
        var c = await Login("laura.jimenez");
        var res = await c.PostAsJsonAsync($"/api/expedientes/{id}/actuaciones", new { type = "Providencia", title = "Auto", body = "No debe pasar" });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task AbogadoNoApruebaPago()
    {
        var c = await Login("felipe.ardila");
        var res = await c.GetAsync("/api/contratos");
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task CrearRadicadoUnicoYPersistente()
    {
        var c = await Login("laura.jimenez");
        var res = await c.PostAsJsonAsync("/api/radicados", new { channel = "Correo", subject = "Acción de tutela de prueba", body = "Tutela ficticia para aceptación." });
        Assert.True(res.IsSuccessStatusCode, await res.Content.ReadAsStringAsync());
        var rec = await res.Content.ReadFromJsonAsync<JsonElement>();
        var number = rec.GetProperty("number").GetString();
        Assert.StartsWith("EDI-", number);
        var id = rec.GetProperty("id").GetGuid();
        var again = await c.GetFromJsonAsync<JsonElement>($"/api/radicados/{id}");
        Assert.Equal(number, again.GetProperty("number").GetString());
        Assert.True(again.GetProperty("isTutela").GetBoolean());
    }

    [Fact]
    public async Task IntendenteNoVeReservado()
    {
        var c = await Login("ivan.cardenas");
        var cases = await c.GetFromJsonAsync<JsonElement>("/api/expedientes?size=80");
        foreach (var item in cases.GetProperty("items").EnumerateArray())
            Assert.NotEqual("Reservado", item.GetProperty("classification").GetString());
    }

    [Fact]
    public async Task TicNoAccedeReservado()
    {
        var tic = await Login("diego.torres");
        Assert.Equal(HttpStatusCode.Forbidden, (await tic.GetAsync("/api/expedientes?size=80")).StatusCode);
        var disc = await Login("roberto.aguilar");
        var cases = await disc.GetFromJsonAsync<JsonElement>("/api/expedientes?size=80");
        var hidden = cases.GetProperty("items").EnumerateArray().First(x => x.GetProperty("classification").GetString() == "Reservado");
        var denied = await tic.GetAsync($"/api/expedientes/{hidden.GetProperty("id").GetGuid()}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task InformeYPdf()
    {
        var c = await Login("claudia.bernal");
        var list = await c.GetFromJsonAsync<JsonElement>("/api/informes");
        var id = list[0].GetProperty("id").GetGuid();
        var gen = await c.PostAsync($"/api/informes/{id}/borrador", null);
        Assert.True(gen.IsSuccessStatusCode, await gen.Content.ReadAsStringAsync());
        var pdf = await c.GetAsync($"/api/informes/{id}/pdf");
        Assert.True(pdf.IsSuccessStatusCode, await pdf.Content.ReadAsStringAsync());
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
    }
}

public sealed class DemoFactory : WebApplicationFactory<Program>
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "edi360-tests-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_dir);
        Environment.SetEnvironmentVariable("EDI_DATA_ROOT", _dir);
        builder.UseSetting("Urls", "http://127.0.0.1:0");
    }
}
