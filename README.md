# FABRIK

FABRIK is Forward and Backward Reaching Inverse Kinematics. It is a fast IK method established by Andreas Aristidou and Joan Lasenby. 
http://andreasaristidou.com/FABRIK.html

This original implementation is derived from the source material.

## Unity Package Manager support /#upm

Add to your project via the Unity Package Manager. 
1. In the Package Manger, select "Add package from Git URL..."
2. Type in 
```
https://github.com/yohash/FABRIK.git#upm
```

The `upm` branch is maintained us a current subtree via:
```
git subtree split --prefix=Assets/ContinuumCrowds --branch upm
```

## Dependencies

This package has a dependency on another custom package. To allow for automatic installion of dependencies
- [yohash.bezier](https://github.com/yohash/Bezier)

Please first install the [mob-sakai/GitDependencyResolverForUnity](https://github.com/mob-sakai/GitDependencyResolverForUnity). The git dependency resolver can be installed in the Unity package manager with this direct git link:
```
https://github.com/mob-sakai/GitDependencyResolverForUnity.git
```
