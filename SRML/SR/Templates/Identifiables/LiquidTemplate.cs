using MonomiPark.SlimeRancher.Regions;
using SRML.SR.Utils.BaseObjects;
using UnityEngine;

namespace SRML.SR.Templates.Identifiables
{
    /// <summary>
    /// A template to create new liquids
    /// </summary>
    public class LiquidTemplate : ModPrefab<LiquidTemplate>
    {
        // Base for Identifiables
        protected Identifiable.Id ID;
        protected Vacuumable.Size vacSize = Vacuumable.Size.NORMAL;

        // The Materials and Color
        protected Material[] materials;
        protected Color color = Color.clear;
        GameObject destroyFX;

        /// <summary>
        /// Template to create new liquids
        /// </summary>
        /// <param name="name">The name of the object (prefixes are recommended, but not needed)</param>
        /// <param name="ID">The Identifiable ID for this liquid</param>
        /// <param name="materials">The materials that compose this liquid's model</param>
        public LiquidTemplate(string name, Identifiable.Id ID, Material[] materials = null) : base(name)
        {
            this.ID = ID;
            this.materials = materials ?? BaseObjects.originMaterial["Depth Water Ball"].Group();
            destroyFX = EffectObjects.fxWaterSplat;
        }

        /// <summary>
        /// Sets the vacuumable size
        /// </summary>
        /// <param name="vacSize">The vac size to set</param>
        public LiquidTemplate SetVacSize(Vacuumable.Size vacSize)
        {
            this.vacSize = vacSize;
            return this;
        }

        /// <summary>
        /// Sets the color for the material (only if you want to use the default material for water and change the color)
        /// <para>DO NOT USE THIS IF YOU SET A CUSTOM MATERIAL, THIS WILL OVERRIDE THAT MATERIAL</para>
        /// </summary>
        /// <param name="color">New color to set</param>
        public LiquidTemplate SetColor(Color color)
        {
            this.color = color;
            return this;
        }

        /// <summary>
        /// Sets the translation for this slime's name
        /// </summary>
        /// <param name="name">The translated name</param>
        public override LiquidTemplate SetTranslation(string name)
        {
            TranslationPatcher.AddActorTranslation("l." + ID.ToString().ToLowerInvariant(), name);
            return this;
        }

        /// <summary>
        /// Creates the object of the template (To get the prefab version use .ToPrefab() after calling this)
        /// </summary>
        public override LiquidTemplate Create()
        {
            // Register as Liquid & Craft neeed Materials
            BuildLiquidMaterialsAndRegister();

            // Create main object
            mainObject.AddComponents(
                new Create<Identifiable>(ident => ident.id = ID),
                new Create<Vacuumable>(vac => vac.size = vacSize),
                new Create<Rigidbody>(body =>
                {
                    body.drag = 0.2f;
                    body.angularDrag = 0.05f;
                    body.mass = 1f;
                    body.useGravity = true;
                }),
                new Create<SphereCollider>(col =>
                {
                    col.center = Vector3.zero;
                    col.radius = 1f;
                }),
                new Create<CollisionAggregator>(null),
                new Create<RegionMember>(rg => rg.canHibernate = true),
                new Create<DestroyOnTouching>(dest =>
                {
                    dest.hoursOfContactAllowed = 0;
                    dest.wateringRadius = 4;
                    dest.wateringUnits = 3;
                    dest.destroyFX = destroyFX;
                    dest.touchingWaterOkay = false;
                    dest.touchingAshOkay = false;
                    dest.reactToActors = false;
                    dest.liquidType = ID;
                })
            );

            // Create sphere
            mainObject.AddChild(new GameObjectTemplate("Sphere",
                new Create<MeshFilter>(filter => filter.sharedMesh = BaseObjects.originMesh["slimeglop"]),
                new Create<MeshRenderer>(render => render.sharedMaterials = materials)
            ).SetTransform(Vector3.zero, Vector3.zero, Vector3.one * 1.5f)
            .AddAfterChildren(AddEffects));

            // Create delaunch
            mainObject.AddChild(new GameObjectTemplate("DelaunchTrigger",
                new Create<SphereCollider>(col =>
                {
                    col.center = Vector3.zero;
                    col.radius = 0.1f;
                    col.isTrigger = true;
                }),
                new Create<VacDelaunchTrigger>(null)
            ));

            mainObject.Layer = BaseObjects.layers["Actor"];

            return this;
        }

        protected virtual void BuildLiquidMaterialsAndRegister()
        {
            if (color != Color.clear)
            {
                var mat = new Material(BaseObjects.originMaterial["Depth Water Ball"]);
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, color);

                mat.SetTexture("_ColorRamp", tex);

                materials = mat.Group();

                var inFX = EffectObjects.fxWaterAcquire.CreatePrefabCopy();
                inFX.name = inFX.name.Replace("(Clone)", "." + mainObject.Name);
                var part = inFX.GetComponent<ParticleSystem>();
                if (part != null)
                {
                    var main = part.main;
                    main.startColor = color;
                }

                var render = inFX.FindChild("Water Glops").GetComponent<ParticleSystemRenderer>();
                if (render != null)
                {
                    render.sharedMaterials = materials;
                }

                foreach (var childPart in inFX.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = childPart.main;
                    main.startColor = color;
                }



                var vacFailFX = EffectObjects.fxWaterVacFail.CreatePrefabCopy();
                vacFailFX.name = vacFailFX.name.Replace("(Clone)", "." + mainObject.Name);
                part = vacFailFX.GetComponent<ParticleSystem>();
                if (part != null)
                {
                    var main = part.main;
                    main.startColor = color;
                }

                foreach (var childPart in vacFailFX.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = childPart.main;
                    main.startColor = color;
                }

                destroyFX = EffectObjects.fxWaterSplat.CreatePrefabCopy();
                destroyFX.name = destroyFX.name.Replace("(Clone)", "." + mainObject.Name);
                part = destroyFX.GetComponent<ParticleSystem>();
                if (part != null)
                {
                    var main = part.main;
                    main.startColor = color;
                }

                render = destroyFX.FindChild("Water Glops").GetComponent<ParticleSystemRenderer>();
                if (render != null)
                {
                    render.sharedMaterials = materials;
                }

                foreach (var childPart in destroyFX.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = childPart.main;
                    main.startColor = color;
                }

                LookupRegistry.RegisterLiquid(new LiquidDefinition()
                {
                    id = ID,
                    inFX = inFX,
                    vacFailFX = vacFailFX
                });
            }
            else
            {
                LookupRegistry.RegisterLiquid(new LiquidDefinition()
                {
                    id = ID,
                    inFX = EffectObjects.fxWaterAcquire,

                    vacFailFX = EffectObjects.fxWaterVacFail
                });
            }
        }

        protected void AddEffects(GameObject obj)
        {
            var fx1 = BaseObjects.originFXs["FX Sprinkler 1"].CreatePrefabCopy();
            var fx2 = BaseObjects.originFXs["FX Water Glops"].CreatePrefabCopy();

            fx1.transform.parent = obj.transform;
            fx2.transform.parent = obj.transform;

            fx1.SetActive(true);
            fx2.SetActive(true);

            if (color != Color.clear)
            {
                var part = fx1.GetComponent<ParticleSystem>();
                if (part != null)
                {
                    var main = part.main;
                    main.startColor = color;
                }

                foreach (var childPart in fx1.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = childPart.main;
                    main.startColor = color;
                }

                part = fx2.GetComponent<ParticleSystem>();
                if (part != null)
                {
                    var main = part.main;
                    main.startColor = color;
                }

                var render = fx2.GetComponent<ParticleSystemRenderer>();
                if (render != null)
                {
                    render.sharedMaterials = materials;
                }

                foreach (var childPart in fx2.GetComponentsInChildren<ParticleSystem>())
                {
                    var main = childPart.main;
                    main.startColor = color;
                }
            }
        }
    }
}
