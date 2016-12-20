using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Security;

namespace LabAutenticacao
{
    /// <summary>
    /// HttpApplication capaz de lidar com autenticação windows (intranet) e forms (extranet) dentro do ambiente
    /// de execução da intranet da empresa
    /// </summary>
    public abstract class MvcMultiAuthApplication : System.Web.HttpApplication
    {
        private static Func<String, String[]> getUserRolesFor;

        /// <summary>
        /// Função para recuperar papéis de usuários autenticados. Deve ser uma função thread safe
        /// </summary>
        /// <param name="getUserRolesFunc"></param>
        public MvcMultiAuthApplication(Func<String, String[]> getUserRolesFunc = null)
        {
            if (MvcMultiAuthApplication.getUserRolesFor == null)
            {
                if (getUserRolesFunc == null)
                    getUserRolesFunc = (s) => new String[] { };

                MvcMultiAuthApplication.getUserRolesFor = getUserRolesFunc;
            }
        }

        #region Implementação eventos da requisição HTTP

        

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
            //Verificando que o usuário foi autenticado via windows auth
            if (Request.IsAuthenticated && HttpContext.Current.User.Identity is WindowsIdentity)
            {
                //Extraindo nome do usuário a partir da identificação enviada [DOMINIO]\[NOM_USUARIO]
                String userName = Regex.Replace(HttpContext.Current.User.Identity.Name, ".*\\\\(.*)", "$1", RegexOptions.None).ToUpper();
                
                //Realizando autenticação do usuário (Muda o tipo de identidade do usuário para FormsIdentity)
                MvcApplication.SignInUser(userName, true);
            }
        }

        protected void Application_AcquireRequestState(Object sender, EventArgs e)
        {
            //
            //Application_AcquireRequestState: Esse evento acontece quando a sessão está disponível para leitura e escrita
            //

            //Não temos em cache informações sobre o usuário autenticado
            if (MvcApplication.AuthenticatedUser == null)
            {
                //Mas já sabemos que o usuário da requisição foi autenticado no passo 'AuthenticateRequest', por isso podemos guardá-lo
                if (HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    MvcApplication.AuthenticatedUser = HttpContext.Current.User;
                }
            }

            //OK, e agora, temos o cache de usuário autenticado?
            if (MvcApplication.AuthenticatedUser != null)
            {
                HttpContext.Current.User = MvcApplication.AuthenticatedUser;
            }
        }

        protected void Application_EndRequest(object sender, EventArgs e)
        {
            // Capturando um redirecionamento para página de login (criado pelo FormsAuthentication)
            if (this.Response.StatusCode == (int)HttpStatusCode.Redirect && this.Response.RedirectLocation.ToLower().Contains("/usuarios/login"))
            {
                //Usuário não está autenticado ainda?
                if (HttpContext.Current.User == null || String.IsNullOrEmpty(HttpContext.Current.User.Identity.AuthenticationType))
                {
                    
#if AUTH_INTRANET_AUTOMATICA

                    //O header X-EXTERNAL-REQUEST deve ser incluido dinamicamente pelo proxy reverso em todas as requisições originadas fora da rede interna
                    if (!Request.Headers.AllKeys.Contains("X-EXTERNAL-REQUEST"))
                    {
                        //Sendo a requisição é interna, vamos trocar o status de resposta para 401, isso forçará o browser enviar informações de autenticação windows na intranet
                        this.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                            
                        //Limpando informações de redicionamento, não precisaremos mais dela já que autenticaremos aqui via windows auth (.net 4.5)
                        this.Response.SuppressFormsAuthenticationRedirect = true;
                    }
#endif
                     
                }
                //No outro caso, ele já está autenticado, possivelmente encontramos um erro de autorização que o FormAuth está tentando redirecionar
                //vamos cancelar esse comportamento, redefinindo o código de retorno para 'Forbidden'
                else if (HttpContext.Current.User.Identity.IsAuthenticated)
                {
                    this.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    this.Response.SuppressFormsAuthenticationRedirect = true;

                    this.Response.Clear();
                }
            }
        }

        #endregion



        /// <summary>
        /// Confirma que o usuário identificado pelo nome informado está autenticado
        /// </summary>
        /// <param name="userName"></param>
        private static Boolean SignInUser(String userName, Boolean intranetAuth)
        {
            //A autenticação sempre será do tipo FormsAuth, portanto vamos preparar o cookie de autenticação 
            HttpCookie formsAuthCookie = FormsAuthentication.GetAuthCookie(userName, false);
            HttpContext.Current.User = new GenericPrincipal(new FormsIdentity(FormsAuthentication.Decrypt(formsAuthCookie.Value)), MvcMultiAuthApplication.getUserRolesFor(userName));

            FormsAuthentication.SetAuthCookie(HttpContext.Current.User.Identity.Name, false);

            //Se esse método for chamado em um contexto que já existe sessão, podemos guardar as definições do utilizador na sessão
            if (HttpContext.Current.Session != null)
            {
                MvcMultiAuthApplication.AuthenticatedUser = HttpContext.Current.User;
            }

            return HttpContext.Current.User.Identity.IsAuthenticated;
        }
                
        /// <summary>
        /// Confirma que o usuário identificado pelo nome informado está autenticado
        /// </summary>
        /// <param name="userName"></param>
        internal static Boolean SignInUser(String userName)
        {
            if (String.IsNullOrEmpty(userName))
                throw new ArgumentException("Identificador de usuário inválido");

            return SignInUser(userName, false);
        }

        /// <summary>
        /// Desativa autenticação do usuário atualmente autenticado
        /// </summary>
        internal static void SignOutCurrentUser()
        {
            HttpCookie authCookie = HttpContext.Current.Request.Cookies[FormsAuthentication.FormsCookieName];

            if (authCookie != null)
            {
                authCookie.Expires = DateTime.Today.AddDays(-1);
            }

            HttpContext.Current.Response.Cookies.Add(authCookie);
            MvcMultiAuthApplication.AuthenticatedUser = null;
            FormsAuthentication.SignOut();
        }

        /// <summary>
        /// Recupera URL padrão ou página de entrada da aplicação
        /// </summary>
        /// <returns></returns>
        internal static String GetApplicationDefaultUrl()
        {
            return FormsAuthentication.DefaultUrl;
        }

        /// <summary>
        /// Recupera URL a qual o sistema redirecionou o usuário para aplicar a autenticação. Caso não 
        /// esteja disponível, retorna a URL padrão recuperada por GetApplicationDefaultUrl()
        /// </summary>
        /// <returns></returns>
        internal static String GetApplicationReturnUrl()
        {
            String urlParaRetornar = HttpContext.Current.Request.QueryString["ReturnUrl"];

            if (!String.IsNullOrEmpty(urlParaRetornar))
                return urlParaRetornar;
            else
                return MvcApplication.GetApplicationDefaultUrl();
        }

        /// <summary>
        /// Recupera se a autenticação sendo realizada é do tipo intranet
        /// </summary>
        /// <returns></returns>
        internal static Boolean? IsIntranetUser()
        {
            throw new NotImplementedException("Aguardando implementação");
        }
        

        /// <summary>
        /// Cache da identidade do usuário autenticado
        /// </summary>
        private static IPrincipal AuthenticatedUser
        {
            get { return HttpContext.Current.Session["MVCAPP_USER_IDENTITY"] as IPrincipal; }
            set { HttpContext.Current.Session["MVCAPP_USER_IDENTITY"] = value; }
        }
    }
}