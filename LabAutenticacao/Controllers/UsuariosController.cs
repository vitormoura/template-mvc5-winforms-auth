using LabAutenticacao.Models;
using System.Web.Mvc;
using System.Web.Security;

namespace LabAutenticacao.Controllers
{
    [Authorize]
    public class UsuariosController : AppController
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            //Para testar essa action em desenvolvimento, ative a autenticação anônima junto as configurações
            //do IIS Express

            if (!User.Identity.IsAuthenticated)
            {
                return View("Login", new FormLogin { Usuario = "ADMIN", LoginURL = FormsAuthentication.LoginUrl });
            }
            else
                return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(FormLogin form)
        {
            if (ModelState.IsValid)
            {
                if (this.ValidarUsuarioSenha(form))
                {
                    //Registra autenticação e retorna ao endereço anterior ou endereço padrão do FormsAuthentication

                    if (MvcApplication.SignInUser(form.Usuario))
                    {
                        return Redirect(FormsAuthentication.DefaultUrl);
                    }
                    else
                        form.Mensagem = "Ocorreu um problema no processo de autenticação, tente novamente";
                }
                else
                    form.Mensagem = "Usuário ou senha inválidos";
            }
            else
                form.Mensagem = "Informe usuário e senha";

            form.LoginURL = FormsAuthentication.LoginUrl;

            return View("Login", form);
        }
                
        public ActionResult Sair()
        {
            MvcApplication.SignOutCurrentUser();

            return View("Sair");
        }

        private bool ValidarUsuarioSenha(FormLogin form)
        {
            //TODO: Realize validação de usuário e senha

            return form.Usuario.ToUpper() == "ADMIN" && form.Senha.ToUpper() == "123";
        }
    }
}