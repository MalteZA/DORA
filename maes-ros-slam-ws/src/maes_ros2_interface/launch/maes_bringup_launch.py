# Copyright 2022 MAES
# 
# This file is part of MAES
# 
# MAES is free software: you can redistribute it and/or modify it under
# the terms of the GNU General Public License as published by the
# Free Software Foundation, either version 3 of the License, or (at your option)
# any later version.
# 
# MAES is distributed in the hope that it will be useful, but WITHOUT
# ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
# or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General
# Public License for more details.
# 
# You should have received a copy of the GNU General Public License along
# with MAES. If not, see http://www.gnu.org/licenses/.
# 
# Contributors: Malte Z. Andreasen, Philip I. Holler and Magnus K. Jensen
# 
# Original repository: https://github.com/MalteZA/MAES
import os

from ament_index_python.packages import get_package_share_directory

from launch import LaunchDescription
from launch.actions import (DeclareLaunchArgument, GroupAction,
                            IncludeLaunchDescription, SetEnvironmentVariable)
from launch.conditions import IfCondition
from launch.launch_description_sources import PythonLaunchDescriptionSource
from launch.substitutions import LaunchConfiguration, PythonExpression
from launch_ros.actions import PushRosNamespace
from nav2_common.launch import ReplaceString


def generate_launch_description():
    # Get the launch directory
    package_name = 'maes_ros2_interface'
    package_dir = get_package_share_directory(package_name)
    bringup_dir = get_package_share_directory('nav2_bringup')

    # Create the launch configuration variables
    namespace = LaunchConfiguration('namespace')
    use_namespace = LaunchConfiguration('use_namespace')
    slam = LaunchConfiguration('slam')
    use_sim_time = LaunchConfiguration('use_sim_time')
    params_file = LaunchConfiguration('params_file')
    autostart = LaunchConfiguration('autostart')
    # Maes params /home/philipholler/RiderProjects/MAES/Assets/maes-ros-slam-ws/install/maes_ros2_interface/share/maes_ros2_interface/navigate_through_poses_w_replanning_and_recovery.xml
    raytrace_range = LaunchConfiguration('raytrace_range')
    robot_radius = LaunchConfiguration('robot_radius')
    # TODO: For some reason the global costmap does not dynamically resize.
    #  Instead we dynamically make the size of the map generated by MAES
    global_costmap_width = LaunchConfiguration('global_costmap_width')
    global_costmap_height = LaunchConfiguration('global_costmap_height')
    global_costmap_origin_x = LaunchConfiguration('global_costmap_origin_x')
    global_costmap_origin_y = LaunchConfiguration('global_costmap_origin_y')

    # Injecting the behavior tree xml file as launch configuration does not seem to work
    # Thus it is done directly with a path. This should maybe be done as a launch configuration in the future
    bt_nav_through_poses_xml_path = os.path.join(os.getcwd(), 'install', package_name, 'share', package_name, 'navigate_through_poses_w_replanning_and_recovery.xml')
    bt_nav_to_pose_xml_path = os.path.join(os.getcwd(), 'install', package_name, 'share', package_name, 'navigate_to_pose_w_replanning_and_recovery.xml')

    maes_injected_params_file = ReplaceString(
        source_file=params_file,
        replacements={'<robot_namespace>': ('', namespace),
                      '<raytrace_range>': ('', raytrace_range),
                      '<robot_radius>': ('', robot_radius),
                      '<global_costmap_width>': ('', global_costmap_width),
                      '<global_costmap_height>': ('', global_costmap_height),
                      '<global_costmap_origin_x>': ('', global_costmap_origin_x),
                      '<global_costmap_origin_y>': ('', global_costmap_origin_y),
                      '<bt_nav_through_poses_xml_path>': ('', bt_nav_through_poses_xml_path),
                      '<bt_nav_to_pose_xml_path>': ('', bt_nav_to_pose_xml_path)})

    stdout_linebuf_envvar = SetEnvironmentVariable(
        'RCUTILS_LOGGING_BUFFERED_STREAM', '1')

    # Maes launch arguments
    declare_raytrace_range_cmd = DeclareLaunchArgument(
        'raytrace_range',
        description='Raytrace range for the given robot')
    declare_robot_radius_cmd = DeclareLaunchArgument(
        'robot_radius',
        description='Radius of the robot. Affects path planning')
    declare_global_costmap_width = DeclareLaunchArgument(
        'global_costmap_width',
        description='Ensures that global costmap is set to the same width as in the MAES simulation')
    declare_global_costmap_height = DeclareLaunchArgument(
        'global_costmap_height',
        description='Ensures that global costmap is set to the same height as in the MAES simulation')
    declare_global_costmap_origin_x = DeclareLaunchArgument(
        'global_costmap_origin_x',
        description='Offsets the map in the x direction to ensure availability until map border')
    declare_global_costmap_origin_y = DeclareLaunchArgument(
        'global_costmap_origin_y',
        description='Offsets the map in the y direction to ensure availability until map border')

    declare_namespace_cmd = DeclareLaunchArgument(
        'namespace',
        default_value='',
        description='Top-level namespace')


    declare_use_namespace_cmd = DeclareLaunchArgument(
        'use_namespace',
        default_value='true',
        description='Whether to apply a namespace to the navigation stack')

    declare_slam_cmd = DeclareLaunchArgument(
        'slam',
        default_value='True',
        description='Whether run a SLAM')

    declare_use_sim_time_cmd = DeclareLaunchArgument(
        'use_sim_time',
        default_value='false',
        description='Use simulation (Gazebo) clock if true')

    declare_params_file_cmd = DeclareLaunchArgument(
        'params_file',
        default_value=os.path.join(bringup_dir, 'params', 'nav2_params.yaml'),
        description='Full path to the ROS2 parameters file to use for all launched nodes')

    declare_autostart_cmd = DeclareLaunchArgument(
        'autostart', default_value='true',
        description='Automatically startup the nav2 stack')

    # Specify the actions
    bringup_cmd_group = GroupAction([
        PushRosNamespace(
            condition=IfCondition(use_namespace),
            namespace=namespace),

        IncludeLaunchDescription(
            PythonLaunchDescriptionSource(os.path.join(package_dir, 'maes_slam_launch.py')),
            condition=IfCondition(slam),
            launch_arguments={'namespace': namespace,
                              'use_sim_time': use_sim_time,
                              'autostart': autostart,
                              'params_file': maes_injected_params_file}.items()),

        IncludeLaunchDescription(
            PythonLaunchDescriptionSource(os.path.join(bringup_dir, 'launch', 'navigation_launch.py')),
            launch_arguments={'namespace': namespace,
                              'use_sim_time': use_sim_time,
                              'autostart': autostart,
                              'params_file': maes_injected_params_file,
                              'use_lifecycle_mgr': 'false',
                              'map_subscribe_transient_local': 'true'}.items()),
    ])

    # Create the launch description and populate
    ld = LaunchDescription()

    # Set environment variables
    ld.add_action(stdout_linebuf_envvar)

    # Declare the launch options
    ld.add_action(declare_namespace_cmd)
    ld.add_action(declare_use_namespace_cmd)
    ld.add_action(declare_slam_cmd)
    ld.add_action(declare_use_sim_time_cmd)
    ld.add_action(declare_params_file_cmd)
    ld.add_action(declare_autostart_cmd)
    # Declare maes launch options
    ld.add_action(declare_raytrace_range_cmd)
    ld.add_action(declare_robot_radius_cmd)
    ld.add_action(declare_global_costmap_width)
    ld.add_action(declare_global_costmap_height)
    ld.add_action(declare_global_costmap_origin_x)
    ld.add_action(declare_global_costmap_origin_y)

    ld.add_action(bringup_cmd_group)

    return ld
