using Godot;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace yyl_sts2_mod.Code.Nodes;

[GlobalClass]
public partial class yylNParticlesContainer : NParticlesContainer
{
    // public override void _Ready()
    // {
    //     base._Ready();
    //     if (_particles != null && _particles.Count != 0) return;
    //     _particles = [];
    //     foreach (var child in GetChildren())
    //         if (child is GpuParticles2D particles)
    //             _particles.Add(particles);
    // }
}