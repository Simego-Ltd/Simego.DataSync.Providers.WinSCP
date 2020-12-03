using System;
using System.Drawing.Design;
using System.Reflection;

namespace Simego.DataSync.Providers.WinSCP.TypeEditors
{
    public class CredentialsTypeEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(System.ComponentModel.ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(System.ComponentModel.ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (FCredentials fCredentials = new FCredentials())
            {
                PropertyInfo fInfoUsername = context.Instance.GetType().GetProperty("UserName");
                PropertyInfo fInfoPassword = context.Instance.GetType().GetProperty("Password");

                fCredentials.Username = (string)fInfoUsername?.GetValue(context.Instance, null) ?? string.Empty;

                if (fCredentials.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    fInfoUsername?.SetValue(context.Instance, fCredentials.Username, null);
                    fInfoPassword?.SetValue(context.Instance, fCredentials.Password, null);
                }

                return fCredentials.Username;
            }
        }

    }
}
