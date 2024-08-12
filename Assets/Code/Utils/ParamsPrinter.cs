using System.Text;
using EasyButtons;
using UnityEngine;

[ExecuteAlways]
public class ParamsPrinter : MonoBehaviour
{
    private StringBuilder _currentMessage = new StringBuilder();

    [Button]
    private void PrintCurrentMessage()
    {
        print(_currentMessage);
        _currentMessage.Clear();
    }
    
    [Button]
    private void AddPos()
    {
        _currentMessage.Append(FormatVector3(transform.position));
    }
    
    //Add local pos
    [Button]
    private void AddLocalPos()
    {
        _currentMessage.Append(FormatVector3(transform.localPosition));
    }
    
    [Button]
    private void AddGlobalRotation()
    {
        _currentMessage.Append(FormatQuaternion(transform.rotation));
    }
    
    [Button]
    private void AddLocalRotation()
    {
        _currentMessage.Append(FormatQuaternion(transform.localRotation));
    }
    
    private void AddPsLifeSpeedSizeGravMod()
    {
        var ps = GetComponent<ParticleSystem>();
        var main = ps.main;
        _currentMessage.Append($"\nmain.startLifetime = {main.startLifetime.constant}f;");
        _currentMessage.Append($"\nmain.startSpeed = {main.startSpeed.constant}f;");
        _currentMessage.Append($"\nmain.startSize = {main.startSize.constant}f;");
        _currentMessage.Append($"\nmain.gravityModifier = {main.gravityModifier.constant}f;");
    }
    
    [Button]
    public void PrintPos()
    {
        print(FormatVector3(transform.position));
    }
    
    [Button]
    public void PrintLocalPos()
    {
        print(FormatVector3(transform.localPosition));
    }
    
    [Button]
    private void PrintGlobalRotation()
    {
        print(FormatQuaternion(transform.rotation));
    }
    
    [Button]
    public void PrintLocalRotation()
    {
        print(FormatQuaternion(transform.localRotation));
    }
    
    [Button]
    public void PrintColor()
    {
        var mat = GetComponent<MeshRenderer>().sharedMaterial;
        print(FormatColor(mat.GetColor("_TintColor")));
    }
    
    [Button]
    public void PrintColorPs()
    {
        var mat = GetComponent<ParticleSystem>().GetComponent<Renderer>().sharedMaterial;
        print(FormatColor(mat.GetColor("_TintColor")));
    }
    
    private string FormatVector3(Vector3 vec3)
    {
        // Format each component to the desired format with 'f' suffix and specified precision
        string x = vec3.x.ToString("0.000") + "f";
        string y = vec3.y.ToString("0.000") + "f";
        string z = vec3.z.ToString("0.000") + "f";

        // Combine the formatted components into the final string
        return $"\nnew Vector3 ({x}, {y}, {z});";
    }
    
    private string FormatColor(Color color)
    {
        // Format each component to the desired format with 'f' suffix and specified precision
        string r = color.r.ToString("0.00000") + "f";
        string g = color.g.ToString("0.00000") + "f";
        string b = color.b.ToString("0.00000") + "f";
        string a = color.a.ToString("0.00000") + "f";

        // Combine the formatted components into the final string
        return $"\n({r}, {g}, {b}, {a})";
    }
    
    private string FormatQuaternion(Quaternion rotation)
    {
        string x = rotation.x.ToString("0.000") + "f";
        string y = rotation.y.ToString("0.000") + "f";
        string z = rotation.z.ToString("0.000") + "f";
        string w = rotation.w.ToString("0.000") + "f";

        // Combine the formatted components into the final string
        return $"\nnew Quaternion ({x}, {y}, {z}, {w});";
    }
}
