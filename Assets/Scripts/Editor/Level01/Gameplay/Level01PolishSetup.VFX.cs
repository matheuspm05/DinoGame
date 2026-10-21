using System;
using ProjetoRetorno.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjetoRetorno.Level01.Editor
{
    public static partial class Level01PolishSetup
    {
        /// <summary>Sem argumentos/retorno. Cria dois prefabs nativos, materiais URP e máscara suave local; nunca altera recursos dos fornecedores.</summary>
        private static void EnsureVFXAssets()
        {
            foreach(var folder in new[]{"Assets/VFX/Particles","Assets/VFX/Materials","Assets/VFX/Prefabs"})Folder(folder);
            const string texturePath="Assets/VFX/Particles/SoftParticle.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if(texture==null)
            {
                texture=new Texture2D(32,32,TextureFormat.RGBA32,false,true){name="SoftParticle",wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[32*32];
                for(int y=0;y<32;y++)for(int x=0;x<32;x++)
                {
                    float radius=new Vector2((x+.5f)/16-1,(y+.5f)/16-1).magnitude;
                    pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2));
                }
                texture.SetPixels(pixels);texture.Apply();AssetDatabase.CreateAsset(texture,texturePath);
            }
            CreateVFX("VFX_EnergyActivation",true,texture);
            CreateVFX("VFX_DoorSteam",false,texture);
        }

        /// <summary>name/energy/texture definem estilo e máscara. Sem retorno. Persiste um prefab não repetitivo, sem colisão, com duração e contagem limitadas.</summary>
        private static void CreateVFX(string name,bool energy,Texture2D texture)
        {
            string path="Assets/VFX/Prefabs/"+name+".prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null)return;
            string materialPath="Assets/VFX/Materials/"+name+"_URP.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null)
            {
                var shader=Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if(shader==null)throw new InvalidOperationException("Shader URP Particles/Unlit não encontrado.");
                material=new Material(shader){name=name+"_URP"};
                material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);
                material.SetFloat("_Surface",1);material.SetFloat("_Blend",0);material.SetFloat("_ZWrite",0);
                material.SetFloat("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetFloat("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_Cull",0);material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType","Transparent");material.renderQueue=(int)RenderQueue.Transparent;
                AssetDatabase.CreateAsset(material,materialPath);
            }
            var root=new GameObject(name);
            try
            {
                var particles=root.AddComponent<ParticleSystem>();particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=particles.main;main.loop=false;main.playOnAwake=false;main.duration=energy?1.5f:.8f;
                main.startLifetime=energy?new ParticleSystem.MinMaxCurve(1.2f,1.8f):new ParticleSystem.MinMaxCurve(.8f,1.5f);
                main.startSpeed=energy?new ParticleSystem.MinMaxCurve(.25f,.7f):new ParticleSystem.MinMaxCurve(.6f,1f);
                main.startSize=energy?new ParticleSystem.MinMaxCurve(.045f,.09f):new ParticleSystem.MinMaxCurve(.22f,.4f);
                main.startColor=energy?new Color(.08f,.75f,1,.9f):new Color(.85f,.9f,.94f,.65f);
                main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=energy?80:32;
                main.useUnscaledTime=false;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
                main.gravityModifier=energy?0:-.035f;
                var emission=particles.emission;emission.rateOverTime=0;
                emission.SetBursts(energy?new[]{new ParticleSystem.Burst(0,28),new ParticleSystem.Burst(.5f,16),new ParticleSystem.Burst(1,12)}:
                    new[]{new ParticleSystem.Burst(0,10),new ParticleSystem.Burst(.25f,6)});
                var shape=particles.shape;shape.shapeType=energy?ParticleSystemShapeType.Sphere:ParticleSystemShapeType.Cone;
                shape.radius=energy?.18f:.06f;shape.angle=energy?22:30;
                var color=particles.colorOverLifetime;color.enabled=true;
                var gradient=new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.6f,.6f),new GradientAlphaKey(0,1)});
                color.color=gradient;
                var size=particles.sizeOverLifetime;size.enabled=true;
                size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.EaseInOut(0,.35f,1,energy?.7f:1.9f));
                var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
                renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.shadowCastingMode=ShadowCastingMode.Off;
                renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
                PrefabUtility.SaveAsPrefabAsset(root,path);
                Report.Add("VFX criado: "+path+"; URP; loop=false; maxParticles="+main.maxParticles+"; sem Collider.");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        /// <summary>parent/name/prefab identificam emissor e fonte real. Retorna ParticleSystem existente ou uma instância com vínculo ao prefab.</summary>
        private static ParticleSystem Effect(Transform parent,string name,string prefab)
        {
            Transform current=parent.Find(name);
            if(current!=null)return current.GetComponent<ParticleSystem>();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VFX/Prefabs/"+prefab+".prefab");
            if(asset==null)throw new InvalidOperationException("Prefab VFX ausente: "+prefab);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,parent);instance.name=name;
            return instance.GetComponent<ParticleSystem>();
        }
    }
}
