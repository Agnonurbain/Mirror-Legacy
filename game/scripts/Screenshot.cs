using Godot;

namespace MirrorChronicles.Game
{
    /// <summary>Saves the window as PNG once the layout has settled, then quits (smoke runs).</summary>
    public static class Screenshot
    {
        private const int SettleFrames = 3;

        public static async void CaptureAndQuit(Node node, string path)
        {
            var tree = node.GetTree();
            for (int i = 0; i < SettleFrames; i++)
                await node.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            var error = node.GetViewport().GetTexture().GetImage().SavePng(path);
            if (error != Error.Ok) GD.PushError($"[Smoke] Cannot save the screenshot to {path}: {error}");
            tree.Quit();
        }
    }
}
