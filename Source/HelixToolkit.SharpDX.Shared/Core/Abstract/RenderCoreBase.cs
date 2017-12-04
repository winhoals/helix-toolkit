﻿using SharpDX;
using SharpDX.Direct3D11;
using System;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX.Core
#else
namespace HelixToolkit.UWP.Core
#endif
{
    /// <summary>
    /// Base class for all render core classes
    /// </summary>
    public abstract class RenderCoreBase : DisposeObject, IRenderCore
    {
        public Guid GUID { get; } = Guid.NewGuid();

        private EffectMatrixVariable mWorldVar;

        public event EventHandler<bool> OnInvalidateRenderer;
        /// <summary>
        /// Model matrix
        /// </summary>
        public Matrix ModelMatrix { set; get; } = Matrix.Identity;      
        public Effect Effect { private set; get; }
        public EffectTechnique EffectTechnique { private set; get; }
        public Device Device { get { return Effect == null ? null : Effect.Device; } }
        /// <summary>
        /// Is render core has been attached
        /// </summary>
        public bool IsAttached { private set; get; } = false;
        /// <summary>
        /// Call to attach the render core.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="technique"></param>
        public void Attach(IRenderTechnique technique)
        {
            if (IsAttached)
            {
                return;
            }
            Effect = technique.Effect;
            EffectTechnique = Effect == null ? null : Effect.GetTechniqueByName(technique.Name);
            IsAttached = OnAttach(technique);
        }

        /// <summary>
        /// During attatching render core. Create all local resources. Use Collect(resource) to let object be released automatically during Detach().
        /// </summary>
        /// <param name="host"></param>
        /// <param name="technique"></param>
        /// <returns></returns>
        protected virtual bool OnAttach(IRenderTechnique technique)
        {            
            mWorldVar = Collect(Effect.GetVariableByName(ShaderVariableNames.WorldMatrix).AsMatrix());
            return true;
        }
        /// <summary>
        /// Detach render core. Release all resources
        /// </summary>
        public void Detach()
        {
            IsAttached = false;
            OnDetach();
        }
        /// <summary>
        /// On detaching, default is to release all resources
        /// </summary>
        protected virtual void OnDetach()
        {
            DisposeAndClear();
        }
        /// <summary>
        /// Trigger OnRender function delegate if CanRender()==true
        /// </summary>
        /// <param name="context"></param>
        public void Render(IRenderMatrices context)
        {
            if (CanRender())
            {
                SetStatesAndVariables(context);
                OnAttachBuffers(context.DeviceContext);
                OnRender(context);
                PostRender(context);
            }
        }

        /// <summary>
        /// Before calling OnRender. Setup commonly used rendering states.
        /// <para>Default to call <see cref="SetShaderVariables"/> and <see cref="SetRasterStates"/></para>
        /// </summary>
        /// <param name="context"></param>
        protected void SetStatesAndVariables(IRenderMatrices context)
        {
            SetRasterStates(context);
            SetShaderVariables(context);
        }

        /// <summary>
        /// Attach vertex buffer routine
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnAttachBuffers(DeviceContext context)
        {
        }

        /// <summary>
        /// Actual render function. Used to attach different render states and call the draw call.
        /// </summary>
        protected abstract void OnRender(IRenderMatrices context);

        /// <summary>
        /// After calling OnRender. Restore some variables, such as HasInstance etc.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void PostRender(IRenderMatrices context) { }

        /// <summary>
        /// Check if can render
        /// </summary>
        /// <returns></returns>
        protected virtual bool CanRender()
        {
            return IsAttached;
        }
        /// <summary>
        /// Set Model World transformation matrix
        /// </summary>
        /// <param name="matrix"></param>
        protected void SetModelWorldMatrix(Matrix matrix)
        {
            mWorldVar.SetMatrix(matrix);
        }
        /// <summary>
        /// Set additional per model shader constant variables such as model->world matrix etc.
        /// </summary>
        protected virtual void SetShaderVariables(IRenderMatrices matrices)
        {
            SetModelWorldMatrix(ModelMatrix * matrices.WorldMatrix);
        }

        protected virtual void SetRasterStates(IRenderMatrices matrices) { }

        protected void InvalidateRenderer(object sender, bool e)
        {
            OnInvalidateRenderer?.Invoke(sender, e);
        }

        public void ResetInvalidateHandler()
        {
            OnInvalidateRenderer = null;
        }
    }
}
