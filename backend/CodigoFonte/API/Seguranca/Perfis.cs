namespace Captacao.Api.Seguranca;

// Nomes dos perfis de acesso da equipe interna.
// Precisam ser "const" para poderem ser usados dentro de atributos,
// por exemplo: [Authorize(Roles = Perfis.Administrador)].
// Os valores devem ser idênticos aos gravados em Atendente.Perfil,
// pois o AutenticacaoService envia esse texto como claim de Role no JWT.
public static class Perfis
{
    public const string Administrador = "Administrador";

    public const string Atendente = "Atendente";
}
