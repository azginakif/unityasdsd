import os
import sys

import bpy


def clear_scene():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)


def import_fbx(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.abspath(path))
    return [obj for obj in bpy.data.objects if obj not in before]


def find_armature(objects):
    for obj in objects:
        if obj.type == "ARMATURE":
            return obj
    return None


def retarget_action(target_armature, anim_path, action_name):
    imported = import_fbx(anim_path)
    source_armature = find_armature(imported)
    if source_armature is None or source_armature.animation_data is None:
        for obj in imported:
            bpy.data.objects.remove(obj, do_unlink=True)
        return None

    source_action = source_armature.animation_data.action
    if source_action is None:
        for obj in imported:
            bpy.data.objects.remove(obj, do_unlink=True)
        return None

    if target_armature.animation_data is None:
        target_armature.animation_data_create()

    scene = bpy.context.scene
    start_frame = int(source_action.frame_range[0])
    end_frame = int(source_action.frame_range[1])
    target_pose_bones = target_armature.pose.bones
    source_pose_bones = source_armature.pose.bones

    for pose_bone in target_pose_bones:
        pose_bone.rotation_mode = "QUATERNION"

    clear_animation(target_armature)
    source_armature.animation_data.action = source_action

    for target_bone in target_pose_bones:
        source_bone = source_pose_bones.get(target_bone.name)
        if source_bone is None:
            continue

        constraint = target_bone.constraints.new(type="COPY_TRANSFORMS")
        constraint.name = "MobilOfl_Retarget"
        constraint.target = source_armature
        constraint.subtarget = source_bone.name
        constraint.target_space = "WORLD"
        constraint.owner_space = "WORLD"

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.select_all(action="DESELECT")
    target_armature.select_set(True)
    bpy.context.view_layer.objects.active = target_armature
    bpy.ops.object.mode_set(mode="POSE")

    scene.frame_set(start_frame)
    bpy.context.view_layer.update()
    bpy.ops.nla.bake(
        frame_start=start_frame,
        frame_end=end_frame,
        step=1,
        only_selected=False,
        visual_keying=True,
        clear_constraints=True,
        use_current_action=False,
        bake_types={"POSE"},
    )
    bpy.ops.object.mode_set(mode="OBJECT")

    target_action = target_armature.animation_data.action
    if target_action is not None:
        target_action.name = action_name
        target_action.use_fake_user = True

    scene.frame_set(start_frame)

    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)

    return target_action


def add_nla_action(armature, action, index):
    if action is None:
        return

    if armature.animation_data is None:
        armature.animation_data_create()

    track = armature.animation_data.nla_tracks.new()
    track.name = action.name
    start_frame = 1 + (index * 120)
    strip = track.strips.new(action.name, start_frame, action)
    strip.action = action
    strip.frame_start = start_frame
    strip.frame_end = start_frame + max(1, int(action.frame_range[1] - action.frame_range[0]))


def clear_animation(armature):
    if armature.animation_data is None:
        armature.animation_data_create()
        return

    armature.animation_data.action = None
    for track in list(armature.animation_data.nla_tracks):
        armature.animation_data.nla_tracks.remove(track)


def main():
    if len(sys.argv) < 7:
        raise SystemExit("Usage: blender --background --python BuildSchoolBoyAnimatedFbx.py -- main idle walk run output")

    args = sys.argv[sys.argv.index("--") + 1 :]
    main_model, idle_path, walk_path, run_path, output_path = args[:5]

    clear_scene()
    # The downloaded Mixamo animation FBXs are "Without Skin" exports in this
    # project: they contain bones and empty mesh objects only. Keep the visible
    # school-boy model as the export base and copy the animation actions onto it.
    main_objects = import_fbx(main_model)
    visible_mesh_vertex_count = sum(len(obj.data.vertices) for obj in main_objects if obj.type == "MESH")
    if visible_mesh_vertex_count <= 0:
        raise RuntimeError("Main school boy model does not contain visible mesh vertices")

    main_armature = find_armature(main_objects)
    if main_armature is None:
        raise RuntimeError("Main school boy armature not found")

    clear_animation(main_armature)

    actions = [
        retarget_action(main_armature, idle_path, "Idle"),
        retarget_action(main_armature, walk_path, "Walk"),
        retarget_action(main_armature, run_path, "Run"),
    ]

    clear_animation(main_armature)

    for index, action in enumerate(actions):
        add_nla_action(main_armature, action, index)

    if main_armature.animation_data is not None and actions[0] is not None:
        main_armature.animation_data.action = actions[0]

    output_dir = os.path.dirname(os.path.abspath(output_path))
    os.makedirs(output_dir, exist_ok=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in main_objects:
        if obj.type in {"ARMATURE", "MESH"} and (obj.type != "MESH" or len(obj.data.vertices) > 0):
            obj.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.abspath(output_path),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=True,
        bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=True,
        bake_anim_use_all_actions=False,
        bake_anim_simplify_factor=0.0,
        path_mode="AUTO",
    )


if __name__ == "__main__":
    main()
